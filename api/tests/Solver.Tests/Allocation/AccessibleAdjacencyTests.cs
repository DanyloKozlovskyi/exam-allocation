using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Solver.Model;
using Solver.Pipeline;
using Solver.Precheck;
using Solver.Validation;
using Xunit;

namespace Solver.Tests.Allocation;

/// <summary>
/// Regression for the accessibility gap found in Phase 1: same-group participants who need accessible seats
/// must be bounded by α over accessible seats only, otherwise stage 1 passes and stage 2 is infeasible.
/// </summary>
public class AccessibleAdjacencyTests
{
    private const string GroupKey = "group";
    private const string GroupValue = "A";
    private static readonly IReadOnlySet<string> AdjacentAccessibleSeats = new HashSet<string> { SeatKeyFormat.Format(0, 0, 0), SeatKeyFormat.Format(0, 1, 0) };

    [Fact]
    public async Task ShouldSplitAccessibleGroupmatesAcrossRoomsWhenAccessibleSeatsAreAdjacent()
    {
        // Arrange — each room: 1×4 single desks, two adjacent accessible seats
        var input = Input(rooms: 2);
        var provider = new ServiceCollection().AddSolver().BuildServiceProvider();

        // Act
        var output = await provider.GetRequiredService<ISolverPipeline>().RunAsync(input, new Progress<SolveProgress>(), CancellationToken.None);

        // Assert
        output.Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
        var best = output.Allocations[0];
        provider.GetRequiredService<IAllocationValidator>().Validate(input, best.Items).IsValid.Should().BeTrue();
        best.Items.Select(x => x.RoomId).Distinct().Should().HaveCount(2, "the two accessible groupmates cannot share a room's adjacent accessible seats");
    }

    [Fact]
    public void ShouldReportNoAdjacencyImpossibleWhenAccessibleSeatsCannotSeparateGroupmates()
    {
        // Arrange — a single such room
        var input = Input(rooms: 1);
        var provider = new ServiceCollection().AddSolver().BuildServiceProvider();

        // Act
        var diagnostics = provider.GetRequiredService<IPrecheck>().Run(input, CancellationToken.None);

        // Assert
        diagnostics.Should().Contain(x => x.Code == DiagnosticCode.NoAdjacencyImpossible);
    }

    [Fact]
    public async Task ShouldCountPinnedAccessibleGroupmatesInTheAccessibleBound()
    {
        // Arrange — P0 is pinned to room 0, so P1 (same group, needs an accessible seat) must go to room 1;
        // the free stage-1 bound alone would allow both in room 0.
        var input = Input(rooms: 2);
        var pinnedRoom = input.Rooms[0].RoomId;
        input = input with { Pins = [new SolverPin(input.Participants[0].ParticipantId, pinnedRoom, SeatKeyFormat.Format(0, 0, 0))] };
        var provider = new ServiceCollection().AddSolver().BuildServiceProvider();

        // Act
        var output = await provider.GetRequiredService<ISolverPipeline>().RunAsync(input, new Progress<SolveProgress>(), CancellationToken.None);

        // Assert
        output.Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
        output.Stats.FeedbackIterations.Should().Be(0, "stage 1 must already respect the accessible α-bound including pins");
        var best = output.Allocations[0];
        provider.GetRequiredService<IAllocationValidator>().Validate(input, best.Items).IsValid.Should().BeTrue();
        best.Items.Single(x => x.ParticipantId == input.Participants[1].ParticipantId).RoomId.Should().NotBe(pinnedRoom);
    }

    private static SolverInput Input(int rooms)
    {
        var solverRooms = Enumerable.Range(0, rooms)
            .Select(_ => new SolverRoom(Guid.NewGuid(), Guid.NewGuid(), 1m, new RoomLayoutModel(1, 4, 1, [], new HashSet<string>(), AdjacentAccessibleSeats)))
            .ToList();
        var participants = Enumerable.Range(0, 2)
            .Select(i => new SolverParticipant(Guid.NewGuid(), new Dictionary<string, string> { [GroupKey] = GroupValue }, true, $"P{i}"))
            .ToList();
        var rule = new SolverRule(Guid.NewGuid(), RuleType.NoAdjacentByAttribute, true, 1, new NoAdjacentByAttributeParameters(GroupKey, [AdjacencyKind.Side], false));
        return new SolverInput(solverRooms, participants, [rule], [], new SolveOptions { NumWorkers = 1, Alternatives = 0, Stage1TimeLimitSeconds = 10 });
    }
}