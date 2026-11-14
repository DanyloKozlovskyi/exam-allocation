using FluentAssertions;
using Solver.Layout;
using Solver.Model;
using Xunit;
using static Solver.Tests.Layout.LayoutFactory;

namespace Solver.Tests.Layout;

/// <summary>
/// 2×2 desks, 2 seats per desk. Seat indices: row 0 → 0,1 (column 0) 2,3 (column 1); row 1 → 4,5 | 6,7.
/// </summary>
public class AdjacencyBuilderTests
{
    private static readonly (int A, int B)[] SameDesk = [(0, 1), (2, 3), (4, 5), (6, 7)];
    private static readonly (int A, int B)[] SideBetweenColumns = [(0, 2), (0, 3), (1, 2), (1, 3), (4, 6), (4, 7), (5, 6), (5, 7)];

    private readonly SeatGenerator _generator = new();
    private readonly AdjacencyBuilder _builder = new();

    public static TheoryData<AdjacencyKind, (int A, int B)[]> AisleCases => new()
    {
        { AdjacencyKind.SameDesk, SameDesk },
        { AdjacencyKind.Side, SameDesk },
        { AdjacencyKind.SideAcrossAisle, [.. SameDesk, .. SideBetweenColumns] },
        { AdjacencyKind.FrontBack, [.. SameDesk, (0, 4), (0, 5), (1, 4), (1, 5), (2, 6), (2, 7), (3, 6), (3, 7)] },
        { AdjacencyKind.Diagonal, SameDesk },
    };

    public static TheoryData<AdjacencyKind, (int A, int B)[]> NoAisleCases => new()
    {
        { AdjacencyKind.Side, [.. SameDesk, .. SideBetweenColumns] },
        { AdjacencyKind.SideAcrossAisle, SameDesk },
        { AdjacencyKind.Diagonal, [.. SameDesk, (0, 6), (0, 7), (1, 6), (1, 7), (2, 4), (2, 5), (3, 4), (3, 5)] },
    };

    [Theory]
    [MemberData(nameof(AisleCases))]
    public void ShouldBuildExactEdgesWithAisleAfterFirstColumn(AdjacencyKind kind, (int A, int B)[] expected)
    {
        // Arrange
        var layout = RoomLayout(2, 2, 2, aisles: [0]);

        // Act
        var edges = Build(layout, [kind], seatLevelSide: false);

        // Assert
        edges.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(NoAisleCases))]
    public void ShouldBuildExactEdgesWithoutAisle(AdjacencyKind kind, (int A, int B)[] expected)
    {
        // Act
        var edges = Build(RoomLayout(2, 2, 2), [kind], seatLevelSide: false);

        // Assert
        edges.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ShouldKeepSameDeskEvenWithoutKinds()
    {
        Build(RoomLayout(2, 2, 2), [], seatLevelSide: false).Should().BeEquivalentTo(SameDesk);
    }

    [Fact]
    public void ShouldConnectOnlyFacingSeatsWhenSeatLevelSide()
    {
        // Act
        var edges = Build(RoomLayout(2, 2, 2), [AdjacencyKind.Side], seatLevelSide: true);

        // Assert
        edges.Should().BeEquivalentTo([.. SameDesk, (1, 2), (5, 6)]);
    }

    [Fact]
    public void ShouldNotApplySeatLevelSideAcrossAisle()
    {
        // Act
        var edges = Build(RoomLayout(2, 2, 2, aisles: [0]), [AdjacencyKind.SideAcrossAisle], seatLevelSide: true);

        // Assert
        edges.Should().BeEquivalentTo([.. SameDesk, .. SideBetweenColumns]);
    }

    [Fact]
    public void ShouldReturnSortedEdgesWithALessThanB()
    {
        // Act
        var edges = _builder.Build(
            RoomLayout(3, 3, 2),
            _generator.Generate(RoomLayout(3, 3, 2)),
            [AdjacencyKind.Side, AdjacencyKind.FrontBack, AdjacencyKind.Diagonal],
            false);

        // Assert
        edges.Should().OnlyContain(e => e.A < e.B);
        edges.Select(e => (e.A * 1000) + e.B).Should().BeInAscendingOrder();
        edges.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ShouldRemoveDisabledSeatsFromEdges()
    {
        // Arrange: 1 row, 3 single-seat desks, middle disabled → remaining seats r0c0 (0) and r0c2 (1) are not neighbours.
        var layout = RoomLayout(1, 3, 1, disabled: [Key(0, 1, 0)]);

        // Act
        var edges = Build(layout, [AdjacencyKind.Side], seatLevelSide: false);

        // Assert
        edges.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReindexEdgesAfterDisabledSeat()
    {
        // Arrange: disabling r0c0s0 shifts every index by one.
        var layout = RoomLayout(1, 2, 2, disabled: [Key(0, 0, 0)]);

        // Act
        var edges = Build(layout, [AdjacencyKind.Side], seatLevelSide: false);

        // Assert: seats r0c0s1=0, r0c1s0=1, r0c1s1=2.
        edges.Should().BeEquivalentTo([(0, 1), (0, 2), (1, 2)]);
    }

    private (int A, int B)[] Build(RoomLayoutModel layout, AdjacencyKind[] kinds, bool seatLevelSide)
        => _builder.Build(layout, _generator.Generate(layout), kinds, seatLevelSide).Select(e => (e.A, e.B)).ToArray();
}