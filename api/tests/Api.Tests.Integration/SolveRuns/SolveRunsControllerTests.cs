using System.Net;
using Api.Constants;
using Api.Dtos;
using Application.Common.Errors;
using Application.Common.Interfaces;
using Domain.Sessions;
using Domain.SolveRuns;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;
using static Api.Tests.Integration.Sessions.ApiTestHelpers;

namespace Api.Tests.Integration.SolveRuns;

public class SolveRunsControllerTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const string RunParam = "runId";
    private const string AllocationParam = "allocationId";
    private const int ConcurrentCreates = 8;
    private const string DummyHash = "0000000000000000000000000000000000000000000000000000000000000000";
    private static readonly TimeSpan HubTimeout = TimeSpan.FromSeconds(30);
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
    public async Task ShouldSolveAndPersistAllocations()
    {
        // Act
        var started = await scenario.Start(alternatives: 1);
        var run = await scenario.WaitForCompletion(started.Id);

        // Assert
        run.Status.Should().Be(SolveRunStatus.Succeeded, run.ErrorMessage);
        run.AllocationIds.Should().NotBeEmpty();
        run.IsStale.Should().BeFalse();
        run.Stats.Should().NotBeNull();

        var allocation = await (await Client.GetAsync(Route(ApiRoutes.Allocations.ById, (AllocationParam, run.AllocationIds[0])))).ReadJson<AllocationDto>();
        allocation.RunId.Should().Be(run.Id);
        allocation.Index.Should().Be(0);
        allocation.Centers.SelectMany(x => x.Rooms).Sum(x => x.Filled).Should().Be(SolveScenario.ParticipantCount);

        var summaries = await (await Client.GetAsync(Route(ApiRoutes.Allocations.ByRun, (RunParam, run.Id)))).ReadJson<List<AllocationSummaryDto>>();
        summaries.Select(x => x.Id).Should().BeEquivalentTo(run.AllocationIds);
    }

    [Fact]
    public async Task ShouldServeSeatingRowsMovesPinsAndExport()
    {
        // Arrange
        var run = await scenario.WaitForCompletion((await scenario.Start(alternatives: 0)).Id);
        run.Status.Should().Be(SolveRunStatus.Succeeded, run.ErrorMessage);
        var allocationId = run.AllocationIds[0];
        var allocation = await (await Client.GetAsync(Route(ApiRoutes.Allocations.ById, (AllocationParam, allocationId)))).ReadJson<AllocationDto>();
        var usedRoom = allocation.Centers.SelectMany(x => x.Rooms).First(x => x.Used);

        // Act — seating of one room
        var seating = await (await Client.GetAsync(Route(ApiRoutes.Allocations.RoomSeating, (AllocationParam, allocationId), (RoomParam, usedRoom.RoomId)))).ReadJson<RoomSeatingDto>();

        // Assert
        seating.Seats.Should().HaveCount(SolveScenario.Rows * SolveScenario.Columns * SolveScenario.SeatsPerDesk);
        seating.Seats.Count(x => x.Participant is not null).Should().Be(usedRoom.Filled);
        seating.Seats.SelectMany(x => x.Violations).Should().NotContain(x => x.IsHard);

        // Act — participants page with search
        var rows = await (await Client.GetAsync(Route(ApiRoutes.Allocations.Participants, (AllocationParam, allocationId)) + "?pageSize=5")).ReadJson<PagedDto<AllocationRowDto>>();
        rows.Total.Should().Be(SolveScenario.ParticipantCount);
        rows.Items.Should().HaveCount(5);
        var searched = await (await Client.GetAsync(Route(ApiRoutes.Allocations.Participants, (AllocationParam, allocationId)) + "?search=Учасник00")).ReadJson<PagedDto<AllocationRowDto>>();
        searched.Total.Should().Be(1);

        // Act — move a participant to a free seat, validate and apply
        var occupied = seating.Seats.First(x => x.Participant is not null);
        var free = seating.Seats.FirstOrDefault(x => x.Participant is null) ?? seating.Seats.Last();
        var moves = new MovesDto([new MoveDto(occupied.Participant!.Id, usedRoom.RoomId, free.SeatKey)]);
        var report = await (await Client.PostJson(Route(ApiRoutes.Allocations.ValidateMoves, (AllocationParam, allocationId)), moves)).ReadJson<ValidationReportDto>();
        report.Violations.Should().NotContain(x => x.Kind == Domain.Allocations.ViolationKind.Unseated);

        var applied = await Client.PostJson(Route(ApiRoutes.Allocations.ApplyMoves, (AllocationParam, allocationId)), moves with { Force = true });
        applied.StatusCode.Should().Be(HttpStatusCode.OK, await applied.Content.ReadAsStringAsync());
        var manual = await applied.ReadJson<AllocationDto>();
        manual.Index.Should().Be(Domain.Allocations.AllocationConstraints.ManualIndex);
        manual.Centers.SelectMany(x => x.Rooms).Sum(x => x.Filled).Should().Be(SolveScenario.ParticipantCount);

        // Act — invalid move
        var invalid = await Client.PostJson(Route(ApiRoutes.Allocations.ValidateMoves, (AllocationParam, allocationId)), new MovesDto([new MoveDto(Guid.NewGuid(), usedRoom.RoomId, free.SeatKey)]));
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await invalid.ReadErrorCode()).Should().Be(ErrorCode.InvalidMove);

        // Act — pin everything
        var pinned = await (await Client.PostJson(Route(ApiRoutes.Pins.PinAll, (AllocationParam, allocationId)), new PinAllDto(null))).ReadJson<List<PinDto>>();
        pinned.Should().HaveCount(SolveScenario.ParticipantCount);
        pinned.Should().OnlyContain(x => x.SeatKey != null);

        // Act — export with real data
        var export = await Client.GetAsync(Route(ApiRoutes.Exports.Export, (AllocationParam, allocationId)) + "?format=Xlsx");
        export.StatusCode.Should().Be(HttpStatusCode.OK);
        (await export.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldMarkSessionStaleAfterRelevantChange()
    {
        // Arrange
        var run = await scenario.WaitForCompletion((await scenario.Start(alternatives: 0)).Id);
        run.Status.Should().Be(SolveRunStatus.Succeeded, run.ErrorMessage);
        var before = await (await Client.GetAsync(Route(ApiRoutes.Sessions.Totals, (SessionParam, scenario.SessionId)))).ReadJson<SessionTotalsDto>();

        // Act
        await SolveScenario.AddParticipant(Client, scenario.SessionId, "Новенький", "КН-1");
        var after = await (await Client.GetAsync(Route(ApiRoutes.Sessions.Totals, (SessionParam, scenario.SessionId)))).ReadJson<SessionTotalsDto>();
        var rerun = await (await Client.GetAsync(Route(ApiRoutes.SolveRuns.ById, (RunParam, run.Id)))).ReadJson<SolveRunDto>();

        // Assert
        before.IsStale.Should().BeFalse();
        after.IsStale.Should().BeTrue();
        rerun.IsStale.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldRejectSecondActiveRunAndCancelQueuedRun()
    {
        // Arrange — a queued run that the worker never sees (inserted directly)
        var queued = SolveRun.New(SolveRunId.New(), new ExamSessionId(scenario.SessionId), new SolveRunOptions(), DummyHash, DateTimeOffset.UtcNow);
        Context.SolveRuns.Add(queued);
        await SaveChangesAsync();

        // Act
        var second = await Client.PostJson(Route(ApiRoutes.SolveRuns.Create, (SessionParam, scenario.SessionId)), new SolveOptionsDto(null, null, null, null, null, null));
        var cancel = await Client.PostAsync(Route(ApiRoutes.SolveRuns.Cancel, (RunParam, queued.Id.Value)), null);
        var cancelled = await (await Client.GetAsync(Route(ApiRoutes.SolveRuns.ById, (RunParam, queued.Id.Value)))).ReadJson<SolveRunDto>();
        var again = await Client.PostAsync(Route(ApiRoutes.SolveRuns.Cancel, (RunParam, queued.Id.Value)), null);

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await second.ReadErrorCode()).Should().Be(ErrorCode.SolveRunAlreadyActive);
        cancel.StatusCode.Should().Be(HttpStatusCode.Accepted);
        cancelled.Status.Should().Be(SolveRunStatus.Cancelled);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await again.ReadErrorCode()).Should().Be(ErrorCode.SolveRunNotCancellable);
    }

    [Fact]
    public async Task ShouldAllowOnlyOneActiveRunPerSessionInTheDatabase()
    {
        // Arrange — two queued runs for one session, written past the handler's check
        var sessionId = new ExamSessionId(scenario.SessionId);
        Context.SolveRuns.Add(SolveRun.New(SolveRunId.New(), sessionId, new SolveRunOptions(), DummyHash, DateTimeOffset.UtcNow));
        Context.SolveRuns.Add(SolveRun.New(SolveRunId.New(), sessionId, new SolveRunOptions(), DummyHash, DateTimeOffset.UtcNow));

        // Act
        var save = () => SaveChangesAsync();

        // Assert
        await save.Should().ThrowAsync<DbUpdateException>();
        Context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task ShouldAnswerConcurrentCreatesWithOneRunOrConflict()
    {
        // Act — parallel creates race the active-run check
        var responses = await Task.WhenAll(Enumerable.Range(0, ConcurrentCreates).Select(_ =>
            Client.PostJson(Route(ApiRoutes.SolveRuns.Create, (SessionParam, scenario.SessionId)), new SolveOptionsDto(0, null, null, null, null, null))));

        // Assert — never a 500; each created run completes
        responses.Select(x => x.StatusCode).Should().OnlyContain(x => x == HttpStatusCode.OK || x == HttpStatusCode.Conflict);
        var created = responses.Where(x => x.StatusCode == HttpStatusCode.OK).ToList();
        created.Should().NotBeEmpty();
        foreach (var response in created)
        {
            var run = await scenario.WaitForCompletion((await response.ReadJson<SolveRunDto>()).Id);
            run.Status.Should().Be(SolveRunStatus.Succeeded, run.ErrorMessage);
        }
    }

    [Fact]
    public async Task ShouldCancelRunningRunThroughItsToken()
    {
        // Arrange — a Running run whose worker token is registered (as the executor does)
        var running = SolveRun.New(SolveRunId.New(), new ExamSessionId(scenario.SessionId), new SolveRunOptions(), DummyHash, DateTimeOffset.UtcNow);
        running.MarkRunning(DateTimeOffset.UtcNow);
        Context.SolveRuns.Add(running);
        await SaveChangesAsync();
        var cancellation = webFactory.Services.GetRequiredService<ISolveRunCancellation>();
        var token = cancellation.Register(running.Id, CancellationToken.None);

        try
        {
            // Act
            var cancel = await Client.PostAsync(Route(ApiRoutes.SolveRuns.Cancel, (RunParam, running.Id.Value)), null);

            // Assert — the token is cancelled; the executor (not the handler) writes the final status
            cancel.StatusCode.Should().Be(HttpStatusCode.Accepted);
            token.IsCancellationRequested.Should().BeTrue();
            var stored = await Context.SolveRuns.AsNoTracking().SingleAsync(x => x.Id == running.Id);
            stored.Status.Should().Be(SolveRunStatus.Running);
        }
        finally
        {
            cancellation.Complete(running.Id);
            await Context.SolveRuns.Where(x => x.Id == running.Id).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task ShouldReportInfeasibleSessionWithDiagnostics()
    {
        // Arrange — more participants than seats
        var crowded = await SolveScenario.Create(Client, groups: 1, groupSize: 1, rooms: 1);
        for (var i = 0; i < SolveScenario.Rows * SolveScenario.Columns * SolveScenario.SeatsPerDesk; i++)
        {
            await SolveScenario.AddParticipant(Client, crowded.SessionId, $"Зайвий{i}", "КН-9");
        }

        try
        {
            // Act
            var precheck = await (await Client.PostAsync(Route(ApiRoutes.SolveRuns.Precheck, (SessionParam, crowded.SessionId)), null)).ReadJson<List<DiagnosticDto>>();
            var run = await crowded.WaitForCompletion((await crowded.Start(alternatives: 0)).Id);

            // Assert
            precheck.Should().Contain(x => x.Code == DiagnosticCode.CapacityTotalExceeded && !string.IsNullOrWhiteSpace(x.Message));
            run.Status.Should().Be(SolveRunStatus.Infeasible);
            run.Diagnostics.Should().Contain(x => x.Code == DiagnosticCode.CapacityTotalExceeded);
            run.AllocationIds.Should().BeEmpty();
        }
        finally
        {
            await crowded.DeleteSession();
        }
    }

    [Fact]
    public async Task ShouldReplayCompletionToLateHubSubscriber()
    {
        // Arrange
        var run = await scenario.WaitForCompletion((await scenario.Start(alternatives: 0)).Id);
        var completed = new TaskCompletionSource<SolveRunDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = Server.GetHubConnection(HubRoutes.SolveProgress);
        connection.On<SolveRunDto>(SolveProgressHubMethods.RunCompleted, dto => completed.TrySetResult(dto));
        await connection.StartAsync();

        // Act
        await connection.InvokeAsync(SolveProgressHubMethods.SubscribeToRun, run.Id);
        var received = await completed.Task.WaitAsync(HubTimeout);

        // Assert
        received.Id.Should().Be(run.Id);
        received.Status.Should().Be(SolveRunStatus.Succeeded);
    }

    [Fact]
    public async Task ShouldPushCompletionToSubscriberDuringRun()
    {
        // Arrange
        var completed = new TaskCompletionSource<SolveRunDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = Server.GetHubConnection(HubRoutes.SolveProgress);
        connection.On<SolveRunDto>(SolveProgressHubMethods.RunCompleted, dto => completed.TrySetResult(dto));
        await connection.StartAsync();

        // Act
        var started = await scenario.Start(alternatives: 0);
        await connection.InvokeAsync(SolveProgressHubMethods.SubscribeToRun, started.Id);
        var received = await completed.Task.WaitAsync(HubTimeout);

        // Assert
        received.Id.Should().Be(started.Id);
        received.Status.Should().Be(SolveRunStatus.Succeeded);
    }

    [Fact]
    public async Task ShouldFailInterruptedRunsOnStartup()
    {
        // Arrange
        var interrupted = SolveRun.New(SolveRunId.New(), new ExamSessionId(scenario.SessionId), new SolveRunOptions(), DummyHash, DateTimeOffset.UtcNow);
        interrupted.MarkRunning(DateTimeOffset.UtcNow);
        Context.SolveRuns.Add(interrupted);
        await SaveChangesAsync();

        // Act
        using (var scope = webFactory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>().InitialiseAsync();
        }

        // Assert
        var stored = await Context.SolveRuns.AsNoTracking().SingleAsync(x => x.Id == interrupted.Id);
        stored.Status.Should().Be(SolveRunStatus.Failed);
        stored.ErrorMessage.Should().Be(ApplicationDbContextInitialiser.InterruptedRunMessage);
        stored.FailureReason.Should().Be(SolveRunFailureReason.Interrupted);
        var dto = await (await Client.GetAsync(Route(ApiRoutes.SolveRuns.ById, (RunParam, interrupted.Id.Value)))).ReadJson<SolveRunDto>();
        dto.FailureReason.Should().Be(SolveRunFailureReason.Interrupted);
    }
}