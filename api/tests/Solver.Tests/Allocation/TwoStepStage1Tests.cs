using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Solver.Allocation;
using Solver.Model;
using Solver.Pipeline;
using Solver.Tests.Pipeline.Fakes;
using Solver.Validation;
using Xunit;

namespace Solver.Tests.Allocation;

/// <summary>Two-step stage 1 of docs/04 §4.1: room-type selection, greedy assignment, evaluation and fallback.</summary>
public class TwoStepStage1Tests
{
    private const int GroupSize = 25;
    private const int Groups = 20;
    private const int AccessibleParticipants = 10;

    [Fact]
    public async Task ShouldOpenTheCheapestRoomSetAndStayValidWithAggregatedRoomTypes()
    {
        // Arrange — 3 centers × 10 rooms of two layouts (identical rooms aggregate into types), R2/R3/R4/R5 rules.
        var input = MixedInput();
        var provider = new ServiceCollection().AddSolver().BuildServiceProvider();

        // Act
        var output = await provider.GetRequiredService<ISolverPipeline>().RunAsync(input, new Progress<SolveProgress>(), CancellationToken.None);

        // Assert
        output.Status.Should().Be(SolveStatus.Optimal);
        var best = output.Allocations[0];
        var report = new AllocationValidator().Validate(input, best.Items);
        report.IsValid.Should().BeTrue();
        report.SoftPenalty.Should().Be(best.Objective.SoftPenalty);
        best.Objective.Cost.Should().Be(MinimumCoveringCost(input));
    }

    [Fact]
    public void ShouldReachTheSelectionLowerBoundInStage1()
    {
        // Arrange
        var input = MixedInput();
        var request = TestSolver.Request(input);

        // Act
        var result = TestSolver.Stage1().Solve(request, new RecordingProgress(), CancellationToken.None);

        // Assert — the cost equals the capacity-knapsack optimum, every hard stage-1 limit holds.
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Objective!.Cost.Should().Be(MinimumCoveringCost(input));
        AssertStage1Limits(request, result.Counts, maxPerRoom: 2);
    }

    [Fact]
    public void ShouldFallBackToTheFullModelWhenCenterSeparationDefeatsTheSelection()
    {
        // Arrange — Center-scoped R2 is not part of room selection: one cheap room cannot take the whole group.
        var cheap = Guid.NewGuid();
        var expensive = Guid.NewGuid();
        var input = new TestInputBuilder()
            .WithRoom(1, 4, 1, cost: 1m, centerId: cheap)
            .WithRoom(1, 4, 1, cost: 1m, centerId: cheap)
            .WithRoom(1, 4, 1, cost: 5m, centerId: expensive)
            .WithGroup("A", 4)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Center, 2))
            .Build();
        var request = TestSolver.Request(input);

        // Act
        var result = TestSolver.Stage1().Solve(request, new RecordingProgress(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Objective!.Cost.Should().Be(6m);
        result.Counts.Where(c => c.RoomId == input.Rooms[2].RoomId).Sum(c => c.Count).Should().Be(2);
    }

    [Fact]
    public void ShouldRespectHardLimitsPinsAndCutsInTheGreedyAssignment()
    {
        // Arrange
        var centerA = Guid.NewGuid();
        var centerB = Guid.NewGuid();
        var builder = new TestInputBuilder();
        for (var i = 0; i < 6; i++)
        {
            builder.WithRoom(2, 3, 1, centerId: i < 3 ? centerA : centerB);
        }

        var input = builder
            .WithGroup("A", 6).WithGroup("B", 9).WithGroup("C", 5).WithGroup("A", 2, needsAccessibleSeat: true)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 3))
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Center, 6))
            .WithRule(RuleType.AccessibleSeating, new AccessibleSeatingParameters())
            .Build()
            .WithAccessibleSeats(0, "r0c0s0")
            .WithAccessibleSeats(4, "r0c0s0");
        var pinned = input.Participants.First(p => p.Attributes[TestInputBuilder.GroupKey] == "B");
        input = input with { Pins = [new SolverPin(pinned.ParticipantId, input.Rooms[1].RoomId, null)] };
        var cut = new Stage1Cut(input.Rooms[2].RoomId, TestInputBuilder.GroupKey, "B", 1);
        var request = TestSolver.Request(input, cuts: [cut]);
        var context = Stage1Context.Create(input, request.Classes, new FakeSeatGenerator(), new FakeIndependentSetCalculator());
        var all = Enumerable.Range(0, input.Rooms.Count).ToHashSet();

        // Act
        var result = GreedyAssignment.Assign(context, all, [cut], CancellationToken.None);

        // Assert
        result.Complete.Should().BeTrue();
        AssertStage1Limits(request, result.Counts, maxPerRoom: 3);
        var classes = request.Classes.ToDictionary(c => c.Index);
        var byCenter = result.Counts
            .GroupBy(c => (Center: input.Rooms.Single(r => r.RoomId == c.RoomId).CenterId, Value: classes[c.ClassIndex].Values[TestInputBuilder.GroupKey]));
        byCenter.Should().OnlyContain(g => g.Sum(c => c.Count) + (g.Key.Center == centerA && g.Key.Value == "B" ? 1 : 0) <= 6);
        result.Counts.Where(c => c.RoomId == cut.RoomId && classes[c.ClassIndex].Values[TestInputBuilder.GroupKey] == "B").Sum(c => c.Count).Should().BeLessThanOrEqualTo(1);
        result.Counts.Where(c => classes[c.ClassIndex].NeedsAccessibleSeat)
            .Should().OnlyContain(c => c.RoomId == input.Rooms[0].RoomId || c.RoomId == input.Rooms[4].RoomId);
    }

    [Fact]
    public void ShouldKeepCenterSeparationAcrossRoomsOfOneCenterInTheGreedyAssignment()
    {
        // Arrange — review P3-1: 8 members of one group, center X with two rooms and center Y with one, at most 5 per center
        // (round-robin over the three rooms would put 6 into X)
        var centerX = Guid.NewGuid();
        var centerY = Guid.NewGuid();
        var builder = new TestInputBuilder();
        for (var i = 0; i < 3; i++)
        {
            builder.WithRoom(2, 3, 1, centerId: i < 2 ? centerX : centerY);
        }

        var input = builder
            .WithGroup("A", 8)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Center, 5))
            .Build();
        var request = TestSolver.Request(input);
        var context = Stage1Context.Create(input, request.Classes, new FakeSeatGenerator(), new FakeIndependentSetCalculator());

        // Act
        var result = GreedyAssignment.Assign(context, Enumerable.Range(0, input.Rooms.Count).ToHashSet(), [], CancellationToken.None);

        // Assert
        result.Complete.Should().BeTrue();
        var centerOf = input.Rooms.ToDictionary(r => r.RoomId, r => r.CenterId);
        result.Counts.GroupBy(c => centerOf[c.RoomId]).Should().OnlyContain(g => g.Sum(c => c.Count) <= 5);
        result.Counts.Sum(c => c.Count).Should().Be(8);
    }

    /// <summary>
    /// 500 participants (20 groups of 25, 10 need an accessible seat) in 3 centers × 10 rooms (5×4×2 = 40 and 6×5×1 = 30 seats,
    /// two accessible seats each) with center costs 1, 2, 3; hard R2 max 2 per room, hard R3, R4, soft R5.
    /// </summary>
    private static SolverInput MixedInput()
    {
        var builder = new TestInputBuilder().WithOptions(o => o with { NumWorkers = 2 });
        for (var c = 0; c < 3; c++)
        {
            var center = Guid.NewGuid();
            for (var r = 0; r < 10; r++)
            {
                if (r % 2 == 0)
                {
                    builder.WithRoom(5, 4, 2, cost: c + 1, centerId: center, aisles: [1]);
                }
                else
                {
                    builder.WithRoom(6, 5, 1, cost: c + 1, centerId: center);
                }
            }
        }

        for (var g = 0; g < Groups; g++)
        {
            var accessible = g < AccessibleParticipants ? 1 : 0;
            builder.WithGroup($"G{g:D2}", GroupSize - accessible);
            if (accessible > 0)
            {
                builder.WithGroup($"G{g:D2}", accessible, needsAccessibleSeat: true);
            }
        }

        var input = builder
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 2))
            .WithRule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(TestInputBuilder.GroupKey, [AdjacencyKind.SameDesk, AdjacencyKind.Side], false))
            .WithRule(RuleType.AccessibleSeating, new AccessibleSeatingParameters())
            .WithRule(RuleType.MinRoomFill, new MinRoomFillParameters(0.5), isHard: false)
            .Build();
        for (var r = 0; r < input.Rooms.Count; r++)
        {
            input = input.WithAccessibleSeats(r, SeatKeyFormat.Format(0, 0, 0), SeatKeyFormat.Format(0, 2, 0));
        }

        return input;
    }

    /// <summary>Independent oracle: minimum cost of rooms whose seats cover everybody and whose accessible seats cover the needs (DP).</summary>
    private static decimal MinimumCoveringCost(SolverInput input)
    {
        var seats = new FakeSeatGenerator();
        var participants = input.Participants.Count;
        var accessible = input.Participants.Count(p => p.NeedsAccessibleSeat);
        var best = new decimal?[participants + 1, accessible + 1];
        best[0, 0] = 0;
        foreach (var room in input.Rooms)
        {
            var generated = seats.Generate(room.Layout);
            var capacity = generated.Count;
            var accessibleSeats = generated.Count(s => s.IsAccessible);
            for (var p = participants; p >= 0; p--)
            {
                for (var a = accessible; a >= 0; a--)
                {
                    if (best[p, a] is not { } cost)
                    {
                        continue;
                    }

                    var np = Math.Min(participants, p + capacity);
                    var na = Math.Min(accessible, a + accessibleSeats);
                    if (best[np, na] is null || best[np, na] > cost + room.Cost)
                    {
                        best[np, na] = cost + room.Cost;
                    }
                }
            }
        }

        return best[participants, accessible]!.Value;
    }

    private static void AssertStage1Limits(Stage1Request request, IReadOnlyList<RoomClassCount> counts, int maxPerRoom)
    {
        var input = request.Input;
        var classes = request.Classes.ToDictionary(c => c.Index);
        var seats = new FakeSeatGenerator();
        foreach (var participantClass in request.Classes)
        {
            counts.Where(c => c.ClassIndex == participantClass.Index).Sum(c => c.Count).Should().Be(participantClass.Size);
        }

        var participants = input.Participants.ToDictionary(p => p.ParticipantId);
        foreach (var room in input.Rooms)
        {
            var generated = seats.Generate(room.Layout);
            var inRoom = counts.Where(c => c.RoomId == room.RoomId).ToList();
            var pins = input.Pins.Where(p => p.RoomId == room.RoomId).Select(p => participants[p.ParticipantId]).ToList();
            (inRoom.Sum(c => c.Count) + pins.Count).Should().BeLessThanOrEqualTo(generated.Count);
            (inRoom.Where(c => classes[c.ClassIndex].NeedsAccessibleSeat).Sum(c => c.Count) + pins.Count(p => p.NeedsAccessibleSeat))
                .Should().BeLessThanOrEqualTo(generated.Count(s => s.IsAccessible));
            var byValue = inRoom
                .Select(c => (Value: classes[c.ClassIndex].Values[TestInputBuilder.GroupKey], c.Count))
                .Concat(pins.Select(p => (Value: p.Attributes[TestInputBuilder.GroupKey], Count: 1)))
                .GroupBy(v => v.Value);
            byValue.Select(g => g.Sum(v => v.Count)).DefaultIfEmpty(0).Max().Should().BeLessThanOrEqualTo(maxPerRoom);
        }
    }
}