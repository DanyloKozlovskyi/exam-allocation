using FluentAssertions;
using Solver.Layout;
using Solver.Model;
using Solver.Seating;
using Xunit;
using static Solver.Tests.Layout.LayoutFactory;

namespace Solver.Tests.Seating;

public class SeatingSolverTests
{
    private readonly SeatingSolver _solver = new();

    [Fact]
    public void ShouldSeatGreedilyInSeatKeyOrderWithoutStage2Rules()
    {
        // Arrange
        var factory = new SeatingRequestFactory(RoomLayout(2, 2, 1)).WithClass("A", 3);

        // Act
        var result = _solver.Solve(factory.Build(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Assignments.Should().Equal(
            new ClassSeatAssignment(0, null, Key(0, 0, 0)),
            new ClassSeatAssignment(0, null, Key(0, 1, 0)),
            new ClassSeatAssignment(0, null, Key(1, 0, 0)));
        result.Penalty.Should().Be(0);
    }

    [Fact]
    public void ShouldBeDeterministicOnGreedyPath()
    {
        // Arrange
        var factory = new SeatingRequestFactory(RoomLayout(3, 4, 2, accessible: [Key(2, 3, 1)]))
            .WithClass("A", 5)
            .WithClass("B", 7)
            .WithClass("C", 1, needsAccessibleSeat: true);
        var request = factory.Build();

        // Act
        var first = _solver.Solve(request, CancellationToken.None);
        var second = _solver.Solve(request, CancellationToken.None);

        // Assert
        second.Assignments.Should().Equal(first.Assignments);
        first.Assignments.Should().Contain(new ClassSeatAssignment(2, null, Key(2, 3, 1)));
        first.Assignments.Where(a => a.ClassIndex == 0).Select(a => a.SeatKey).Should().Equal(
            Key(0, 0, 0), Key(0, 0, 1), Key(0, 1, 0), Key(0, 1, 1), Key(0, 2, 0));
        first.Assignments.Where(a => a.ClassIndex == 1).Select(a => a.SeatKey).Should().Equal(
            Key(0, 2, 1), Key(0, 3, 0), Key(0, 3, 1), Key(1, 0, 0), Key(1, 0, 1), Key(1, 1, 0), Key(1, 1, 1));
    }

    [Fact]
    public void ShouldPlaceAccessibleClassesOnlyOnAccessibleSeats()
    {
        // Arrange
        var accessible = new[] { Key(1, 0, 0), Key(1, 1, 0) };
        var factory = new SeatingRequestFactory(RoomLayout(2, 3, 1, accessible: accessible))
            .WithClass("A", 2, needsAccessibleSeat: true)
            .WithClass("A", 3)
            .WithNoAdjacent(false, AdjacencyKind.Side);

        // Act
        var result = _solver.Solve(factory.Build(), CancellationToken.None);

        // Assert
        result.Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
        result.Assignments.Where(a => a.ClassIndex == 0).Select(a => a.SeatKey).Should().BeEquivalentTo(accessible);
        result.Assignments.Should().HaveCount(5);
    }

    [Fact]
    public void ShouldSatisfyHardNoAdjacencyAndCounts()
    {
        // Arrange
        AdjacencyKind[] kinds = [AdjacencyKind.Side, AdjacencyKind.FrontBack];
        var factory = new SeatingRequestFactory(RoomLayout(4, 4, 2, aisles: [1]))
            .WithClass("A", 7)
            .WithClass("B", 7)
            .WithClass("C", 6)
            .WithClass("A", 1)
            .WithNoAdjacent(true, kinds);

        // Act
        var result = _solver.Solve(factory.Build(), CancellationToken.None);

        // Assert
        result.Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
        AssertOccupancy(factory, result);
        factory.SameGroupAdjacentPairs(result, kinds).Should().Be(0);
    }

    [Fact]
    public void ShouldDetectInfeasibleHardNoAdjacency()
    {
        // Arrange: α = 2 for a 1×4 row of single seats with Side.
        var factory = new SeatingRequestFactory(RoomLayout(1, 4, 1))
            .WithClass("A", 3)
            .WithNoAdjacent(true, AdjacencyKind.Side);

        // Act
        var result = _solver.Solve(factory.Build(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Infeasible);
        result.Assignments.Should().BeEmpty();
    }

    [Fact]
    public void ShouldDetectTooFewSeats()
    {
        // Arrange
        var factory = new SeatingRequestFactory(RoomLayout(1, 2, 1)).WithClass("A", 3);

        // Act
        var result = _solver.Solve(factory.Build(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Infeasible);
    }

    [Fact]
    public void ShouldMinimizeSoftNoAdjacencyViolations()
    {
        // Arrange
        const int weight = 7;
        var factory = new SeatingRequestFactory(RoomLayout(1, 4, 1))
            .WithClass("A", 3)
            .WithRule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(SeatingRequestFactory.GroupKey, [AdjacencyKind.Side], false), isHard: false, weight: weight);
        var request = factory.Build();

        // Act
        var result = _solver.Solve(request, CancellationToken.None);

        // Assert: best possible is one violating pair.
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Penalty.Should().Be(weight);
        result.PenaltyByRule.Should().ContainKey(request.Rules[0].RuleId).WhoseValue.Should().Be(weight);
        factory.SameGroupAdjacentPairs(result, AdjacencyKind.Side).Should().Be(1);
    }

    [Fact]
    public void ShouldSpreadParticipantsInRoom()
    {
        // Arrange
        var factory = new SeatingRequestFactory(RoomLayout(1, 5, 1))
            .WithClass("A", 2)
            .WithClass("B", 1)
            .WithRule(RuleType.SpreadInRoom, new SpreadInRoomParameters([AdjacencyKind.Side]), isHard: false);
        var request = factory.Build();

        // Act
        var result = _solver.Solve(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        factory.OccupiedAdjacentPairs(result, AdjacencyKind.Side).Should().Be(0);
        result.PenaltyByRule[request.Rules[0].RuleId].Should().Be(0);
    }

    [Fact]
    public void ShouldFillFromFront()
    {
        // Arrange
        const int weight = 3;
        var factory = new SeatingRequestFactory(RoomLayout(3, 2, 1))
            .WithClass("A", 3)
            .WithRule(RuleType.FillFromFront, new FillFromFrontParameters(), isHard: false, weight: weight);

        // Act
        var result = _solver.Solve(factory.Build(), CancellationToken.None);

        // Assert: two seats in row 0, one in row 1 → Σ row = 1.
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Assignments.Select(a => SeatKeyParser.SortKey(a.SeatKey).Row).Should().BeEquivalentTo([0, 0, 1]);
        result.Penalty.Should().Be(weight);
    }

    [Fact]
    public void ShouldRespectFixedSeatPinAndRuleAroundIt()
    {
        // Arrange: pinned A at the left end; another A must not sit next to it.
        var factory = new SeatingRequestFactory(RoomLayout(1, 3, 1));
        var pinned = factory.WithPin("A", Key(0, 0, 0));
        factory.WithClass("A", 1).WithClass("B", 1).WithNoAdjacent(true, AdjacencyKind.Side);

        // Act
        var result = _solver.Solve(factory.Build(), CancellationToken.None);

        // Assert
        result.Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
        result.Assignments.Should().Contain(new ClassSeatAssignment(null, pinned, Key(0, 0, 0)));
        result.Assignments.Should().Contain(new ClassSeatAssignment(0, null, Key(0, 2, 0)));
        result.Assignments.Should().Contain(new ClassSeatAssignment(1, null, Key(0, 1, 0)));
    }

    [Fact]
    public void ShouldSeatRoomOnlyPinAsOwnUnit()
    {
        // Arrange
        var factory = new SeatingRequestFactory(RoomLayout(1, 3, 1, accessible: [Key(0, 2, 0)]));
        var pinned = factory.WithPin("B", null, needsAccessibleSeat: true);
        factory.WithClass("A", 2);

        // Act
        var result = _solver.Solve(factory.Build(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Optimal);
        result.Assignments.Should().Contain(new ClassSeatAssignment(null, pinned, Key(0, 2, 0)));
        result.Assignments.Count(a => a.ClassIndex == 0).Should().Be(2);
    }

    [Fact]
    public void ShouldReportInfeasibleForPinOnMissingSeat()
    {
        // Arrange
        var factory = new SeatingRequestFactory(RoomLayout(1, 3, 1, disabled: [Key(0, 1, 0)]));
        factory.WithPin("A", Key(0, 1, 0));

        // Act
        var result = _solver.Solve(factory.Build(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(SolveStatus.Infeasible);
    }

    [Fact]
    public void ShouldDifferFromPreviousSeating()
    {
        // Arrange
        var factory = new SeatingRequestFactory(RoomLayout(2, 3, 1)).WithClass("A", 2).WithClass("B", 2);
        var first = _solver.Solve(factory.Build(), CancellationToken.None);
        factory.WithPrevious(first.Assignments);

        // Act
        var second = _solver.Solve(factory.Build(seed: SolverDefaults.RandomSeed + 1), CancellationToken.None);

        // Assert
        second.Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
        AssertOccupancy(factory, second);
        second.Assignments.Should().NotBeEquivalentTo(first.Assignments);
    }

    [Fact]
    public void ShouldSolveRoomsConcurrently()
    {
        // Arrange
        AdjacencyKind[] kinds = [AdjacencyKind.Side];
        var factories = Enumerable.Range(0, 6)
            .Select(_ => new SeatingRequestFactory(RoomLayout(3, 4, 2, aisles: [1])).WithClass("A", 6).WithClass("B", 6).WithNoAdjacent(true, kinds))
            .ToList();
        var results = new SeatingResult[factories.Count];

        // Act
        Parallel.For(0, factories.Count, i => results[i] = _solver.Solve(factories[i].Build(), CancellationToken.None));

        // Assert
        for (var i = 0; i < factories.Count; i++)
        {
            results[i].Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
            AssertOccupancy(factories[i], results[i]);
            factories[i].SameGroupAdjacentPairs(results[i], kinds).Should().Be(0);
        }
    }

    [Fact]
    public void ShouldReturnCancelledWhenTokenIsCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var factory = new SeatingRequestFactory(RoomLayout(1, 4, 1)).WithClass("A", 2).WithNoAdjacent(true, AdjacencyKind.Side);

        // Act
        var result = _solver.Solve(factory.Build(), cts.Token);

        // Assert
        result.Status.Should().Be(SolveStatus.Cancelled);
    }

    private static void AssertOccupancy(SeatingRequestFactory factory, SeatingResult result)
    {
        var validKeys = new SeatGenerator().Generate(factory.Room.Layout).Select(s => s.Key).ToHashSet();
        result.Assignments.Select(a => a.SeatKey).Should().OnlyHaveUniqueItems().And.OnlyContain(k => validKeys.Contains(k));
        foreach (var participantClass in factory.Classes)
        {
            result.Assignments.Count(a => a.ClassIndex == participantClass.Index).Should().Be(participantClass.Size);
        }
    }
}