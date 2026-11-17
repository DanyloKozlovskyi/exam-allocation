using System.Net;
using Api.Constants;
using Api.Dtos;
using Api.Tests.Integration.SolveRuns;
using Application.Solving.Mapping;
using Application.Solving.Services;
using Domain.Participants;
using Domain.Rules;
using Domain.Sessions;
using Domain.SolveRuns;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Solver.Model;
using Tests.Common;
using static Api.Tests.Integration.Sessions.ApiTestHelpers;
using DomainObjectiveMode = Domain.SolveRuns.ObjectiveMode;
using SolverObjectiveMode = Solver.Model.ObjectiveMode;

namespace Api.Tests.Integration.Solving;

/// <summary>I2 mapping tests: Domain snapshot → <see cref="SolverInput"/> and <see cref="SolverOutput"/> → allocations.</summary>
public class SolverInputBuilderTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const int TestNumWorkers = 1;
    private const int TestStage2SecondsPerRoom = 2;
    private readonly IntegrationTestWebFactory webFactory = factory;
    private SolveScenario scenario = null!;

    public async Task InitializeAsync()
    {
        scenario = await SolveScenario.Create(Client);
    }

    public async Task DisposeAsync()
    {
        await scenario.DeleteSession();
    }

    [Fact]
    public async Task ShouldMapSessionToSolverInput()
    {
        // Arrange — a disabled rule (must be skipped) and a seat pin
        var disabled = new RuleDto(null, scenario.SessionId, Domain.Rules.RuleType.FillFromFront, false, RuleConstraints.DefaultWeight, false, null, new FillFromFrontParametersDto());
        (await Client.PostJson(Route(ApiRoutes.Rules.Create, (SessionParam, scenario.SessionId)), disabled)).StatusCode.Should().Be(HttpStatusCode.OK);
        var sessionId = new ExamSessionId(scenario.SessionId);
        var participant = await Context.Participants.AsNoTracking().Where(x => x.SessionId == sessionId).OrderBy(x => x.Id).FirstAsync();
        var pinnedSeat = SeatKeyFormat.Format(0, 0, 0);
        (await Client.PostJson(Route(ApiRoutes.Pins.Create, (SessionParam, scenario.SessionId)), new PinDto(null, participant.Id.Value, scenario.RoomIds[0], pinnedSeat)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var options = new SolveRunOptions { Alternatives = 3, DiversityRatio = 0.2, CostSlack = 0.3, TimeLimitSeconds = 30, ObjectiveMode = DomainObjectiveMode.Weighted, Seed = 7 };

        // Act
        var input = await Build(builder => builder.Build(sessionId, options, CancellationToken.None));

        // Assert — rooms with layout and the center's cost per room
        input.Rooms.Select(x => x.RoomId).Should().BeEquivalentTo(scenario.RoomIds);
        input.Rooms.Should().OnlyContain(x => x.CenterId == scenario.CenterId && x.Cost == 1m
            && x.Layout.Rows == SolveScenario.Rows && x.Layout.Columns == SolveScenario.Columns && x.Layout.SeatsPerDesk == SolveScenario.SeatsPerDesk);

        // participants with the built-in group attribute
        input.Participants.Should().HaveCount(SolveScenario.ParticipantCount);
        input.Participants.Should().OnlyContain(x => x.Attributes.ContainsKey(AttributeKeys.Group));
        input.Participants.Single(x => x.ParticipantId == participant.Id.Value).Attributes[AttributeKeys.Group].Should().Be(participant.Group);

        // only enabled rules, typed parameters
        input.Rules.Should().ContainSingle().Which.Parameters.Should().BeOfType<NoAdjacentByAttributeParameters>()
            .Which.AttributeKey.Should().Be(AttributeKeys.Group);

        // pin with its seat key
        input.Pins.Should().ContainSingle().Which.Should().Be(new SolverPin(participant.Id.Value, scenario.RoomIds[0], pinnedSeat));

        // run options + server settings (appsettings.Test.json)
        input.Options.Should().BeEquivalentTo(new
        {
            Alternatives = 3,
            DiversityRatio = 0.2,
            CostSlack = 0.3,
            Stage1TimeLimitSeconds = 30,
            RandomSeed = 7,
            ObjectiveMode = SolverObjectiveMode.Weighted,
            NumWorkers = TestNumWorkers,
            Stage2TimeLimitSecondsPerRoom = TestStage2SecondsPerRoom,
        });
    }

    [Fact]
    public async Task ShouldMapSolverOutputToAllocationsWithSeatIds()
    {
        // Arrange — every participant on a distinct seat of room 1, then room 2
        var sessionId = new ExamSessionId(scenario.SessionId);
        var participants = await Context.Participants.AsNoTracking().Where(x => x.SessionId == sessionId).OrderBy(x => x.Id).Select(x => x.Id).ToListAsync();
        var seats = await Context.Seats.AsNoTracking().Where(x => scenario.RoomIds.Select(r => new Domain.Rooms.RoomId(r)).Contains(x.RoomId))
            .OrderBy(x => x.RoomId).ThenBy(x => x.Key).ToListAsync();
        var items = participants.Select((id, i) => new SeatAssignment(id.Value, seats[i].RoomId.Value, seats[i].Key.Value)).ToList();
        var objective = new ObjectiveBreakdown(1, 1m, 5, new Dictionary<Guid, long>());
        var output = new SolverOutput(SolveStatus.Optimal, [new SolvedAllocation(0, items, objective), new SolvedAllocation(1, items, objective)], [], new SolveStats());
        var runId = SolveRunId.New();

        // Act
        var allocations = await Build(builder => builder.ToAllocations(runId, sessionId, output, CancellationToken.None));

        // Assert
        allocations.Select(x => x.Index).Should().Equal(0, 1);
        allocations.Should().OnlyContain(x => x.RunId == runId && x.Objective.SoftPenalty == 5 && x.Objective.Cost == 1m);
        var mapped = allocations[0].Items.ToDictionary(x => x.ParticipantId, x => x.SeatId);
        for (var i = 0; i < participants.Count; i++)
        {
            mapped[participants[i]].Should().Be(seats[i].Id);
        }
    }

    [Fact]
    public async Task ShouldRejectUnknownSeatInSolverOutput()
    {
        // Arrange
        var sessionId = new ExamSessionId(scenario.SessionId);
        var participant = await Context.Participants.AsNoTracking().Where(x => x.SessionId == sessionId).Select(x => x.Id).FirstAsync();
        var items = new[] { new SeatAssignment(participant.Value, scenario.RoomIds[0], SeatKeyFormat.Format(99, 99, 0)) };
        var output = new SolverOutput(SolveStatus.Feasible, [new SolvedAllocation(0, items, new ObjectiveBreakdown(1, 1m, 0, new Dictionary<Guid, long>()))], [], new SolveStats());

        // Act
        var act = () => Build(builder => builder.ToAllocations(SolveRunId.New(), sessionId, output, CancellationToken.None));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(SolveStatus.Optimal, SolveRunStatus.Succeeded)]
    [InlineData(SolveStatus.Feasible, SolveRunStatus.Succeeded)]
    [InlineData(SolveStatus.Infeasible, SolveRunStatus.Infeasible)]
    [InlineData(SolveStatus.Cancelled, SolveRunStatus.Cancelled)]
    [InlineData(SolveStatus.NoSolutionInTime, SolveRunStatus.Failed)]
    [InlineData(SolveStatus.Failed, SolveRunStatus.Failed)]
    public void ShouldMapSolverStatusToRunStatus(SolveStatus status, SolveRunStatus expected)
    {
        // Act / Assert
        SolverModelMapper.ToRunStatus(status).Should().Be(expected);
    }

    [Fact]
    public void ShouldRoundTripDiagnostics()
    {
        // Arrange
        var diagnostic = new Diagnostic(
            Solver.Model.DiagnosticCode.SeparationImpossible,
            Solver.Model.DiagnosticSeverity.Error,
            Guid.NewGuid(),
            new Dictionary<Solver.Model.DiagnosticArgument, string> { [Solver.Model.DiagnosticArgument.Value] = "КН-1", [Solver.Model.DiagnosticArgument.Size] = "12" });

        // Act
        var roundTrip = SolverModelMapper.ToSolverDiagnostic(SolverModelMapper.ToDomainDiagnostic(diagnostic));

        // Assert
        roundTrip.Should().BeEquivalentTo(diagnostic);
    }

    private async Task<T> Build<T>(Func<ISolverInputBuilder, Task<T>> action)
    {
        using var scope = webFactory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<ISolverInputBuilder>());
    }
}