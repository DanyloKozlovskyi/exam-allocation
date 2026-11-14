using System.Diagnostics;
using FluentAssertions;
using Solver.Model;
using Solver.Tests.Allocation;
using Solver.Tests.Pipeline.Fakes;
using Xunit;

namespace Solver.Tests.Pipeline;

public class SolverPipelineTests
{
    private static readonly NoAdjacentByAttributeParameters NoAdjacentGroups =
        new(TestInputBuilder.GroupKey, [AdjacencyKind.SameDesk, AdjacencyKind.Side, AdjacencyKind.FrontBack], false);

    [Fact]
    public async Task ShouldProduceValidAllocationForSmallInput()
    {
        // Arrange — builder equivalent of DataGen small (default rule set of docs/08).
        var input = SmallInput();
        var progress = new RecordingProgress();

        // Act
        var output = await TestSolver.Pipeline().RunAsync(input, progress, CancellationToken.None);

        // Assert
        output.Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
        var best = output.Allocations.Should().ContainSingle().Subject;
        best.Index.Should().Be(0);
        new FakeValidator().Validate(input, best.Items).IsValid.Should().BeTrue();
        best.Items.Should().HaveCount(input.Participants.Count);
        best.Objective.RoomsUsed.Should().Be(best.Items.Select(i => i.RoomId).Distinct().Count());
        output.Stats.Classes.Should().BeGreaterThan(0);
        output.Stats.Variables.Should().BeGreaterThan(0);
        output.Stats.Constraints.Should().BeGreaterThan(0);
        progress.Reports.Select(p => p.Stage).Distinct().Should().Contain([SolveStage.Precheck, SolveStage.Stage1, SolveStage.Stage2, SolveStage.Expanding, SolveStage.Done]);
        progress.Reports.Where(p => p.Stage == SolveStage.Stage2).Should().Contain(p => p.RoomsDone == p.RoomsTotal);
    }

    [Fact]
    public async Task ShouldSeparateGroupsAcrossRooms()
    {
        // Arrange — docs/08: 3 groups × 4, 4 rooms × 3 seats, hard R2 max 1.
        var builder = new TestInputBuilder();
        for (var i = 0; i < 4; i++)
        {
            builder.WithRoom(1, 3, 1);
        }

        var input = builder.WithGroup("A", 4).WithGroup("B", 4).WithGroup("C", 4)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 1))
            .Build();

        // Act
        var output = await TestSolver.Pipeline().RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Status.Should().Be(SolveStatus.Optimal);
        new FakeValidator().Validate(input, output.Allocations[0].Items).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldSeatNonAdjacentGroupAcrossTwoRooms()
    {
        // Arrange — docs/08: 1×4 single-seat desks, group of 3, two rooms → valid.
        var input = new TestInputBuilder()
            .WithRoom(1, 4, 1).WithRoom(1, 4, 1)
            .WithGroup("A", 3)
            .WithRule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(TestInputBuilder.GroupKey, [AdjacencyKind.Side], false))
            .Build();

        // Act
        var output = await TestSolver.Pipeline().RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
        new FakeValidator().Validate(input, output.Allocations[0].Items).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldSeatAccessibleParticipantsOnAccessibleSeatsAndRespectPins()
    {
        // Arrange
        var builder = new TestInputBuilder().WithRoom(2, 3, 1).WithRoom(2, 3, 1).WithGroup("A", 5).WithGroup("B", 2, needsAccessibleSeat: true);
        var draft = builder.Build();
        var pinnedSeat = SeatKeyFormat.Format(1, 2, 0);
        var input = builder.WithPin(draft.Participants[0].ParticipantId, draft.Rooms[1].RoomId, pinnedSeat).Build()
            .WithAccessibleSeats(0, "r0c0s0", "r0c1s0");

        // Act
        var output = await TestSolver.Pipeline().RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
        var items = output.Allocations[0].Items;
        new FakeValidator().Validate(input, items).IsValid.Should().BeTrue();
        items.Should().Contain(new SeatAssignment(draft.Participants[0].ParticipantId, draft.Rooms[1].RoomId, pinnedSeat));
        var accessible = input.Participants.Where(p => p.NeedsAccessibleSeat).Select(p => p.ParticipantId).ToHashSet();
        items.Where(i => accessible.Contains(i.ParticipantId)).Should().OnlyContain(i => i.RoomId == input.Rooms[0].RoomId && i.SeatKey.EndsWith("s0") && i.SeatKey.StartsWith("r0"));
    }

    [Fact]
    public async Task ShouldStopAfterPrecheckErrors()
    {
        // Arrange
        var input = new TestInputBuilder().WithRoom(1, 2, 1).WithGroup("A", 3).Build();
        var stage1 = new RecordingStage1Solver(TestSolver.Stage1());

        // Act
        var output = await TestSolver.Pipeline(stage1: stage1).RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Status.Should().Be(SolveStatus.Infeasible);
        output.Allocations.Should().BeEmpty();
        output.Diagnostics.Should().Contain(d => d.Code == DiagnosticCode.CapacityTotalExceeded);
        stage1.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldExplainInfeasibilityWithConflictCore()
    {
        // Arrange — docs/08 conflict case.
        var input = ConflictExplainerTests.ConflictInput(out var roomRule, out var centerRule, out _);

        // Act
        var output = await TestSolver.Pipeline().RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Status.Should().Be(SolveStatus.Infeasible);
        var core = output.Diagnostics.Should().ContainSingle(d => d.Code == DiagnosticCode.ConflictCore).Subject;
        core.Arguments[DiagnosticArgument.RuleIds].Should().Contain(roomRule.ToString()).And.Contain(centerRule.ToString());
    }

    [Fact]
    public async Task ShouldAddFeedbackCutWhenRoomSeatingFails()
    {
        // Arrange
        var input = new TestInputBuilder()
            .WithRoom(2, 3, 1).WithRoom(2, 3, 1).WithRoom(2, 3, 1)
            .WithGroup("A", 4).WithGroup("B", 4)
            .WithRule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(TestInputBuilder.GroupKey, [AdjacencyKind.Side], false))
            .Build();
        var stage1 = new RecordingStage1Solver(TestSolver.Stage1());
        var seating = new FailingSeatingSolver(new FakeSeatingSolver(), failures: 1);

        // Act
        var output = await TestSolver.Pipeline(seating: seating, stage1: stage1).RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
        output.Stats.FeedbackIterations.Should().BeGreaterThanOrEqualTo(1);
        stage1.Requests.Should().HaveCountGreaterThan(1);
        stage1.Requests[^1].Cuts.Should().NotBeEmpty();
        stage1.Requests[^1].Hint.Should().NotBeNull();
        new FakeValidator().Validate(input, output.Allocations[0].Items).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReportSeatingInfeasibleAfterMaxFeedbackIterations()
    {
        // Arrange
        var input = new TestInputBuilder()
            .WithRoom(2, 4, 1).WithRoom(2, 4, 1)
            .WithGroup("A", 6)
            .WithRule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(TestInputBuilder.GroupKey, [AdjacencyKind.Side], false))
            .WithOptions(o => o with { MaxFeedbackIterations = 2 })
            .Build();
        var seating = new FailingSeatingSolver(new FakeSeatingSolver(), failures: int.MaxValue);

        // Act
        var output = await TestSolver.Pipeline(seating: seating).RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Status.Should().Be(SolveStatus.Infeasible);
        output.Allocations.Should().BeEmpty();
        output.Diagnostics.Should().Contain(d => d.Code == DiagnosticCode.SeatingInfeasible);
        output.Stats.FeedbackIterations.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task ShouldFailWhenPostValidationFails()
    {
        // Arrange
        var input = new TestInputBuilder().WithRoom(1, 4, 1).WithGroup("A", 3).Build();

        // Act
        var output = await TestSolver.Pipeline(validator: new FakeValidator(alwaysInvalid: true)).RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Status.Should().Be(SolveStatus.Failed);
        output.Allocations.Should().BeEmpty();
        output.Diagnostics.Should().ContainSingle(d => d.Code == DiagnosticCode.InternalValidationFailed && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task ShouldReturnCancelledWithinTwoSecondsWhenCancelledDuringStage1()
    {
        // Arrange — docs/08 cancellation case.
        var input = Stage1SolverTests.LargeInput(rooms: 60, groups: 40);
        using var cts = new CancellationTokenSource();
        var progress = new RecordingProgress(p =>
        {
            if (p.Stage == SolveStage.Stage1)
            {
                cts.Cancel();
            }
        });
        var stopwatch = Stopwatch.StartNew();

        // Act
        var output = await TestSolver.Pipeline().RunAsync(input, progress, cts.Token);

        // Assert
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        output.Status.Should().Be(SolveStatus.Cancelled);
        output.Allocations.Should().BeEmpty();
    }

    internal static SolverInput SmallInput()
    {
        var random = new Random(3);
        var builder = new TestInputBuilder();
        var centers = new[] { Guid.NewGuid(), Guid.NewGuid() };
        for (var i = 0; i < 6; i++)
        {
            builder.WithRoom(i % 2 == 0 ? 5 : 6, i % 2 == 0 ? 4 : 5, 2, cost: 1m, centerId: centers[i % 2]);
        }

        for (var g = 0; g < 5; g++)
        {
            builder.WithGroup("G" + g, 25 + random.Next(0, 8));
        }

        var input = builder
            .WithGroup("G0", 2, needsAccessibleSeat: true)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 7))
            .WithRule(RuleType.NoAdjacentByAttribute, NoAdjacentGroups)
            .WithRule(RuleType.AccessibleSeating, new AccessibleSeatingParameters())
            .WithRule(RuleType.MinRoomFill, new MinRoomFillParameters(0.5), isHard: false)
            .Build();
        return input.WithAccessibleSeats(0, "r0c0s0", "r0c2s0", "r2c0s0");
    }
}