using System.Diagnostics;
using FluentAssertions;
using Solver.Allocation;
using Solver.Model;
using Solver.Tests.Pipeline.Fakes;
using Xunit;

namespace Solver.Tests.Allocation;

public class Stage1SolverTests
{
    private static readonly RecordingProgress NoProgress = new();

    [Theory]
    [InlineData(20, 6)]
    [InlineData(13, 4)]
    [InlineData(24, 6)]
    public void ShouldOpenMinimumNumberOfRoomsWhenCostFirst(int participants, int roomCapacity)
    {
        // Arrange
        var builder = new TestInputBuilder();
        for (var i = 0; i < 8; i++)
        {
            builder.WithRoom(1, roomCapacity, 1);
        }

        var input = builder.WithGroup("A", participants / 2).WithGroup("B", participants - (participants / 2)).Build();
        var request = TestSolver.Request(input);

        // Act
        var result = TestSolver.Stage1().Solve(request, NoProgress, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Objective!.RoomsUsed.Should().Be((int)Math.Ceiling(participants / (double)roomCapacity));
        result.Objective.Cost.Should().Be(result.Objective.RoomsUsed);
        AssertCoverageAndCapacity(request, result);
    }

    [Fact]
    public void ShouldPreferCheaperRooms()
    {
        // Arrange
        var input = new TestInputBuilder()
            .WithRoom(1, 4, 1, cost: 5m)
            .WithRoom(1, 2, 1, cost: 1m)
            .WithRoom(1, 2, 1, cost: 1m)
            .WithGroup("A", 4)
            .Build();

        // Act
        var result = TestSolver.Stage1().Solve(TestSolver.Request(input), NoProgress, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Objective!.Cost.Should().Be(2m);
        result.Counts.Should().NotContain(c => c.RoomId == input.Rooms[0].RoomId);
    }

    [Fact]
    public void ShouldSeparateGroupsPerRoomWhenHard()
    {
        // Arrange — docs/08: 3 groups × 4 people, 4 rooms × 3 seats, maxPerScope = 1.
        var builder = new TestInputBuilder();
        for (var i = 0; i < 4; i++)
        {
            builder.WithRoom(1, 3, 1);
        }

        var input = builder
            .WithGroup("A", 4).WithGroup("B", 4).WithGroup("C", 4)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 1))
            .Build();
        var request = TestSolver.Request(input);

        // Act
        var result = TestSolver.Stage1().Solve(request, NoProgress, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        AssertCoverageAndCapacity(request, result);
        PerValueAndRoom(request, result).Values.Should().OnlyContain(v => v <= 1);
    }

    [Fact]
    public void ShouldSeparateGroupsPerCenterWhenHard()
    {
        // Arrange
        var centerA = Guid.NewGuid();
        var centerB = Guid.NewGuid();
        var input = new TestInputBuilder()
            .WithRoom(1, 4, 1, centerId: centerA).WithRoom(1, 4, 1, centerId: centerA)
            .WithRoom(1, 4, 1, centerId: centerB).WithRoom(1, 4, 1, centerId: centerB)
            .WithGroup("A", 4).WithGroup("B", 4)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Center, 2))
            .Build();
        var request = TestSolver.Request(input);

        // Act
        var result = TestSolver.Stage1().Solve(request, NoProgress, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        var centerOf = input.Rooms.ToDictionary(r => r.RoomId, r => r.CenterId);
        result.Counts
            .GroupBy(c => (centerOf[c.RoomId], request.Classes[c.ClassIndex].Values[TestInputBuilder.GroupKey]))
            .Should().OnlyContain(g => g.Sum(c => c.Count) <= 2);
    }

    [Fact]
    public void ShouldBoundSameValueByIndependentSetSize()
    {
        // Arrange — 1×4 single-seat desks with Side adjacency: α = 2.
        var input = new TestInputBuilder()
            .WithRoom(1, 4, 1).WithRoom(1, 4, 1)
            .WithGroup("A", 3)
            .WithRule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(TestInputBuilder.GroupKey, [AdjacencyKind.Side], false))
            .Build();

        // Act
        var result = TestSolver.Stage1().Solve(TestSolver.Request(input), NoProgress, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Counts.Should().OnlyContain(c => c.Count <= 2);
        result.Objective!.RoomsUsed.Should().Be(2);
    }

    [Fact]
    public void ShouldPutAccessibleClassesOnlyInRoomsWithAccessibleSeats()
    {
        // Arrange
        var input = new TestInputBuilder()
            .WithRoom(1, 4, 1).WithRoom(1, 4, 1)
            .WithGroup("A", 2).WithGroup("A", 2, needsAccessibleSeat: true)
            .Build()
            .WithAccessibleSeats(1, "r0c0s0", "r0c1s0");
        var request = TestSolver.Request(input);

        // Act
        var result = TestSolver.Stage1().Solve(request, NoProgress, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Counts.Where(c => request.Classes[c.ClassIndex].NeedsAccessibleSeat)
            .Should().OnlyContain(c => c.RoomId == input.Rooms[1].RoomId && c.Count <= 2);
    }

    [Fact]
    public void ShouldForcePinnedRoomsOpenAndCountPins()
    {
        // Arrange
        var builder = new TestInputBuilder().WithRoom(1, 3, 1).WithRoom(1, 3, 1, cost: 10m).WithGroup("A", 3);
        var draft = builder.Build();
        var input = builder.WithPin(draft.Participants[0].ParticipantId, draft.Rooms[1].RoomId).Build();
        var request = TestSolver.Request(input);

        // Act
        var result = TestSolver.Stage1().Solve(request, NoProgress, CancellationToken.None);

        // Assert
        // The pinned (expensive) room is forced open; with 2 free seats there it is also the cheapest choice.
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Objective!.Cost.Should().Be(10m);
        result.Counts.Should().OnlyContain(c => c.RoomId == draft.Rooms[1].RoomId);
        result.Counts.Sum(c => c.Count).Should().Be(2);
        AssertCoverageAndCapacity(request, result);
    }

    [Fact]
    public void ShouldMinimizeShortfallWhenMinRoomFillIsSoft()
    {
        // Arrange — cost-first opens 2 rooms of 6; fill ≥ ⌈0.6·6⌉ = 4 each is achievable with 10 people.
        var input = new TestInputBuilder()
            .WithRoom(1, 6, 1).WithRoom(1, 6, 1).WithRoom(1, 6, 1)
            .WithGroup("A", 10)
            .WithRule(RuleType.MinRoomFill, new MinRoomFillParameters(0.6), isHard: false)
            .Build();

        // Act
        var result = TestSolver.Stage1().Solve(TestSolver.Request(input), NoProgress, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Objective!.RoomsUsed.Should().Be(2);
        result.Objective.SoftPenalty.Should().Be(0);
        result.Counts.GroupBy(c => c.RoomId).Should().OnlyContain(g => g.Sum(c => c.Count) >= 4);
    }

    [Fact]
    public void ShouldKeepValuesTogetherInCenterWhenSoft()
    {
        // Arrange
        var centerA = Guid.NewGuid();
        var centerB = Guid.NewGuid();

        // 3 groups × 6 in rooms of 4 → 5 rooms; every group spans 2 rooms and can still stay in one center.
        var input = new TestInputBuilder()
            .WithRoom(1, 4, 1, centerId: centerA).WithRoom(1, 4, 1, centerId: centerA).WithRoom(1, 4, 1, centerId: centerA)
            .WithRoom(1, 4, 1, centerId: centerB).WithRoom(1, 4, 1, centerId: centerB).WithRoom(1, 4, 1, centerId: centerB)
            .WithGroup("A", 6).WithGroup("B", 6).WithGroup("C", 6)
            .WithRule(RuleType.KeepTogetherInCenter, new KeepTogetherInCenterParameters(TestInputBuilder.GroupKey), isHard: false)
            .Build();
        var request = TestSolver.Request(input);

        // Act
        var result = TestSolver.Stage1().Solve(request, NoProgress, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Objective!.RoomsUsed.Should().Be(5);
        result.Objective.SoftPenalty.Should().Be(0);
        var centerOf = input.Rooms.ToDictionary(r => r.RoomId, r => r.CenterId);
        result.Counts
            .GroupBy(c => request.Classes[c.ClassIndex].Values[TestInputBuilder.GroupKey])
            .Should().OnlyContain(g => g.Select(c => centerOf[c.RoomId]).Distinct().Count() == 1);
    }

    [Fact]
    public void ShouldPenalizeSoftSeparationExcess()
    {
        // Arrange — one room only, group of 3 with soft max 1 → excess 2 · weight 7.
        var input = new TestInputBuilder()
            .WithRoom(1, 4, 1)
            .WithGroup("A", 3)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 1), isHard: false, weight: 7)
            .Build();

        // Act
        var result = TestSolver.Stage1().Solve(TestSolver.Request(input), NoProgress, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Objective!.SoftPenalty.Should().Be(14);
        result.Objective.PenaltyByRule[input.Rules[0].RuleId].Should().Be(14);
    }

    [Fact]
    public void ShouldReachSameCostInWeightedMode()
    {
        // Arrange
        var builder = new TestInputBuilder();
        for (var i = 0; i < 5; i++)
        {
            builder.WithRoom(1, 5, 1);
        }

        var input = builder
            .WithGroup("A", 7).WithGroup("B", 6)
            .WithRule(RuleType.MinRoomFill, new MinRoomFillParameters(0.6), isHard: false)
            .WithOptions(o => o with { ObjectiveMode = ObjectiveMode.Weighted })
            .Build();

        // Act
        var result = TestSolver.Stage1().Solve(TestSolver.Request(input), NoProgress, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Objective!.RoomsUsed.Should().Be(3);
        result.Objective.SoftPenalty.Should().Be(0);
    }

    [Fact]
    public void ShouldRespectCutsAndCostBound()
    {
        // Arrange
        var input = new TestInputBuilder()
            .WithRoom(1, 4, 1).WithRoom(1, 4, 1).WithRoom(1, 4, 1)
            .WithGroup("A", 4)
            .Build();
        var cut = new Stage1Cut(input.Rooms[0].RoomId, TestInputBuilder.GroupKey, "A", 1);
        var noKeyInput = input with { Rules = [new SolverRule(Guid.NewGuid(), RuleType.SeparateByAttribute, false, 1, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 4))] };

        // Act
        var result = TestSolver.Stage1().Solve(TestSolver.Request(noKeyInput, cuts: [cut], costBound: 200), NoProgress, CancellationToken.None);
        var tooTight = TestSolver.Stage1().Solve(TestSolver.Request(noKeyInput, costBound: 50), NoProgress, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Counts.Where(c => c.RoomId == input.Rooms[0].RoomId).Sum(c => c.Count).Should().BeLessThanOrEqualTo(1);
        tooTight.Status.Should().Be(SolveStatus.Infeasible);
    }

    [Fact]
    public void ShouldMoveEnoughParticipantsAgainstPreviousSolutions()
    {
        // Arrange
        var input = new TestInputBuilder()
            .WithRoom(1, 5, 1).WithRoom(1, 5, 1).WithRoom(1, 5, 1).WithRoom(1, 5, 1)
            .WithGroup("A", 5).WithGroup("B", 5)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 3))
            .WithOptions(o => o with { DiversityRatio = 0.3 })
            .Build();
        var solver = TestSolver.Stage1();
        var first = solver.Solve(TestSolver.Request(input), NoProgress, CancellationToken.None);

        // Act
        var second = solver.Solve(TestSolver.Request(input, previous: [first.Counts], costBound: 400), NoProgress, CancellationToken.None);

        // Assert
        second.Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
        Distance(first.Counts, second.Counts).Should().BeGreaterThanOrEqualTo(2 * 3);
    }

    [Fact]
    public void ShouldAbortWithModelTooLargeAboveGuardrail()
    {
        // Arrange — 2 001 classes × 1 000 rooms > 2 000 000 cells.
        var builder = new TestInputBuilder();
        for (var i = 0; i < 1000; i++)
        {
            builder.WithRoom(1, 2, 1);
        }

        var input = builder.Build();
        var classes = Enumerable.Range(0, 2001)
            .Select(i => new ParticipantClass(i, new Dictionary<string, string>(), false, [Guid.NewGuid()]))
            .ToList();

        // Act
        var result = TestSolver.Stage1().Solve(new Stage1Request(input, classes, [], [], null, null), NoProgress, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Failed);
        result.Diagnostics.Should().ContainSingle(d => d.Code == DiagnosticCode.ModelTooLarge && d.Arguments.ContainsKey(DiagnosticArgument.Limit));
    }

    [Fact]
    public void ShouldReportThrottledProgressAndStopWhenCancelled()
    {
        // Arrange
        var input = LargeInput(rooms: 60, groups: 40);
        using var cts = new CancellationTokenSource();
        var progress = new RecordingProgress(p => cts.Cancel());
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = TestSolver.Stage1().Solve(TestSolver.Request(input), progress, cts.Token);

        // Assert
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        result.Status.Should().Be(SolveStatus.Cancelled);
        progress.Reports.Should().NotBeEmpty().And.OnlyContain(p => p.Stage == SolveStage.Stage1);
    }

    [Fact]
    public void ShouldStopSearchQuicklyOnCancellation()
    {
        // Arrange
        var input = LargeInput(rooms: 80, groups: 60);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = TestSolver.Stage1().Solve(TestSolver.Request(input), NoProgress, cts.Token);

        // Assert
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
        if (cts.IsCancellationRequested)
        {
            result.Status.Should().Be(SolveStatus.Cancelled);
        }
    }

    internal static SolverInput LargeInput(int rooms, int groups)
    {
        var random = new Random(7);
        var builder = new TestInputBuilder();
        var centers = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        for (var i = 0; i < rooms; i++)
        {
            builder.WithRoom(1, 10 + random.Next(0, 15), 1, cost: 1m + random.Next(0, 4), centerId: centers[i % centers.Count]);
        }

        for (var g = 0; g < groups; g++)
        {
            builder.WithGroup("G" + g, 10 + random.Next(0, 15));
        }

        return builder
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 4), isHard: false)
            .WithRule(RuleType.KeepTogetherInCenter, new KeepTogetherInCenterParameters(TestInputBuilder.GroupKey), isHard: false)
            .WithRule(RuleType.MinRoomFill, new MinRoomFillParameters(0.9), isHard: false)
            .WithOptions(o => o with { Stage1TimeLimitSeconds = 30 })
            .Build();
    }

    private static int Distance(IReadOnlyList<RoomClassCount> a, IReadOnlyList<RoomClassCount> b)
    {
        var left = a.ToDictionary(c => (c.ClassIndex, c.RoomId), c => c.Count);
        var right = b.ToDictionary(c => (c.ClassIndex, c.RoomId), c => c.Count);
        return left.Keys.Union(right.Keys).Sum(k => Math.Abs(left.GetValueOrDefault(k) - right.GetValueOrDefault(k)));
    }

    private static Dictionary<(Guid RoomId, string Value), int> PerValueAndRoom(Stage1Request request, Stage1Result result)
        => result.Counts
            .GroupBy(c => (c.RoomId, request.Classes[c.ClassIndex].Values[TestInputBuilder.GroupKey]))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Count));

    private static void AssertCoverageAndCapacity(Stage1Request request, Stage1Result result)
    {
        foreach (var participantClass in request.Classes)
        {
            result.Counts.Where(c => c.ClassIndex == participantClass.Index).Sum(c => c.Count).Should().Be(participantClass.Size);
        }

        var seats = new FakeSeatGenerator();
        foreach (var room in request.Input.Rooms)
        {
            var load = result.Counts.Where(c => c.RoomId == room.RoomId).Sum(c => c.Count) + request.Input.Pins.Count(p => p.RoomId == room.RoomId);
            load.Should().BeLessThanOrEqualTo(seats.Generate(room.Layout).Count);
        }
    }
}