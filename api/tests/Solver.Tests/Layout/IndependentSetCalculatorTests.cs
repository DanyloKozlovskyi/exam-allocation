using FluentAssertions;
using Solver.Layout;
using Solver.Model;
using Xunit;
using static Solver.Tests.Layout.LayoutFactory;

namespace Solver.Tests.Layout;

public class IndependentSetCalculatorTests
{
    private readonly IndependentSetCalculator _calculator = new();

    public static TheoryData<RoomLayoutModel, AdjacencyKind[], bool, int> KnownValues => new()
    {
        { RoomLayout(1, 4, 1), [AdjacencyKind.Side], false, 2 },
        { RoomLayout(1, 4, 1), [], false, 4 },
        { RoomLayout(1, 5, 1), [AdjacencyKind.Side], false, 3 },
        { RoomLayout(2, 2, 2, aisles: [0]), [AdjacencyKind.SameDesk], false, 4 },
        { RoomLayout(2, 2, 2), [AdjacencyKind.Side, AdjacencyKind.FrontBack], false, 2 },
        { RoomLayout(1, 2, 2), [AdjacencyKind.Side], true, 2 },
        { RoomLayout(1, 3, 2), [AdjacencyKind.Side], true, 3 },
        { RoomLayout(1, 3, 2), [AdjacencyKind.Side], false, 2 },
        { RoomLayout(3, 3, 1), [AdjacencyKind.Side, AdjacencyKind.FrontBack], false, 5 },
        { RoomLayout(3, 3, 1), [AdjacencyKind.Side, AdjacencyKind.FrontBack, AdjacencyKind.Diagonal], false, 4 },
        { RoomLayout(1, 4, 1, disabled: [Key(0, 1, 0)]), [AdjacencyKind.Side], false, 2 },
        { RoomLayout(1, 5, 1, disabled: [Key(0, 2, 0)]), [AdjacencyKind.Side], false, 2 },
    };

    [Theory]
    [MemberData(nameof(KnownValues))]
    public void ShouldComputeKnownAlpha(RoomLayoutModel layout, AdjacencyKind[] kinds, bool seatLevelSide, int expected)
    {
        _calculator.Calculate(layout, kinds, seatLevelSide, CancellationToken.None).Should().Be(expected);
    }

    [Fact]
    public void ShouldReturnCachedValueForEquivalentLayout()
    {
        // Arrange
        var first = RoomLayout(6, 5, 2, aisles: [1]);
        var second = RoomLayout(6, 5, 2, aisles: [1]);
        AdjacencyKind[] kinds = [AdjacencyKind.Side, AdjacencyKind.FrontBack];

        // Act
        var a = _calculator.Calculate(first, kinds, false, CancellationToken.None);
        var b = _calculator.Calculate(second, [AdjacencyKind.FrontBack, AdjacencyKind.Side, AdjacencyKind.SameDesk], false, CancellationToken.None);

        // Assert
        b.Should().Be(a);
        a.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldBeSafeToCallConcurrently()
    {
        // Arrange
        var layouts = Enumerable.Range(1, 8).Select(n => RoomLayout(n, 4, 1)).ToList();
        var results = new int[layouts.Count * 4];

        // Act
        Parallel.For(0, results.Length, i => results[i] = _calculator.Calculate(layouts[i % layouts.Count], [AdjacencyKind.Side], false, CancellationToken.None));

        // Assert: a 1×4 row with Side has α = 2, so n rows give 2n.
        for (var i = 0; i < results.Length; i++)
        {
            results[i].Should().Be(2 * ((i % layouts.Count) + 1));
        }
    }

    [Fact]
    public void ShouldThrowWhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _calculator.Calculate(RoomLayout(2, 2, 1), [AdjacencyKind.Side], false, cts.Token);

        // Assert
        act.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void ShouldProduceValidGreedyIndependentSet()
    {
        // Arrange
        var layout = RoomLayout(4, 4, 2);
        var seats = new SeatGenerator().Generate(layout);
        var edges = new AdjacencyBuilder().Build(layout, seats, [AdjacencyKind.Side, AdjacencyKind.FrontBack], false);

        // Act
        var chosen = IndependentSetCalculator.Greedy(seats.Count, edges);

        // Assert
        edges.Should().NotContain(e => chosen[e.A] && chosen[e.B]);
        chosen.Count(x => x).Should().BeGreaterThan(0);
    }
}