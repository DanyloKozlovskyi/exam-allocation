using Solver.Layout;
using Solver.Model;

namespace Solver.Tests.Pipeline.Fakes;

/// <summary>Trivial seat generator for WS-A tests: every (row, column, seat) minus disabled seats, in key order.</summary>
public sealed class FakeSeatGenerator : ISeatGenerator
{
    public IReadOnlyList<SolverSeat> Generate(RoomLayoutModel layout)
    {
        var seats = new List<SolverSeat>();
        for (var row = 0; row < layout.Rows; row++)
        {
            for (var column = 0; column < layout.Columns; column++)
            {
                for (var seat = 0; seat < layout.SeatsPerDesk; seat++)
                {
                    var key = SeatKeyFormat.Format(row, column, seat);
                    if (!layout.DisabledSeats.Contains(key))
                    {
                        seats.Add(new SolverSeat(key, row, column, seat, layout.AccessibleSeats.Contains(key)));
                    }
                }
            }
        }

        return seats;
    }
}

/// <summary>Straightforward adjacency per 03 → Seat adjacency (SameDesk always on).</summary>
public sealed class FakeAdjacencyBuilder : IAdjacencyBuilder
{
    public IReadOnlyList<SeatEdge> Build(RoomLayoutModel layout, IReadOnlyList<SolverSeat> seats, IReadOnlyCollection<AdjacencyKind> kinds, bool seatLevelSide)
    {
        var edges = new List<SeatEdge>();
        for (var i = 0; i < seats.Count; i++)
        {
            for (var j = i + 1; j < seats.Count; j++)
            {
                if (Adjacent(layout, seats[i], seats[j], kinds, seatLevelSide))
                {
                    edges.Add(new SeatEdge(i, j));
                }
            }
        }

        return edges;
    }

    private static bool Adjacent(RoomLayoutModel layout, SolverSeat a, SolverSeat b, IReadOnlyCollection<AdjacencyKind> kinds, bool seatLevelSide)
    {
        var rowDelta = Math.Abs(a.Row - b.Row);
        var columnDelta = Math.Abs(a.Column - b.Column);
        var aisle = columnDelta == 1 && layout.AislesAfterColumns.Contains(Math.Min(a.Column, b.Column));
        if (rowDelta == 0 && columnDelta == 0)
        {
            return true;
        }

        if (rowDelta == 0 && columnDelta == 1)
        {
            if (aisle)
            {
                return kinds.Contains(AdjacencyKind.SideAcrossAisle);
            }

            if (!kinds.Contains(AdjacencyKind.Side))
            {
                return false;
            }

            if (!seatLevelSide)
            {
                return true;
            }

            var (left, right) = a.Column < b.Column ? (a, b) : (b, a);
            return left.SeatIndex == layout.SeatsPerDesk - 1 && right.SeatIndex == 0;
        }

        if (columnDelta == 0 && rowDelta == 1)
        {
            return kinds.Contains(AdjacencyKind.FrontBack);
        }

        return rowDelta == 1 && columnDelta == 1 && !aisle && kinds.Contains(AdjacencyKind.Diagonal);
    }
}

/// <summary>Greedy independent set (a lower bound of α, which 04 §3.2 allows).</summary>
public sealed class FakeIndependentSetCalculator : IIndependentSetCalculator
{
    private readonly FakeSeatGenerator seatGenerator = new();
    private readonly FakeAdjacencyBuilder adjacencyBuilder = new();

    public static List<int>[] Neighbours(IReadOnlyList<SeatEdge> edges, int count)
    {
        var neighbours = Enumerable.Range(0, count).Select(_ => new List<int>()).ToArray();
        foreach (var edge in edges)
        {
            neighbours[edge.A].Add(edge.B);
            neighbours[edge.B].Add(edge.A);
        }

        return neighbours;
    }

    public int Calculate(RoomLayoutModel layout, IReadOnlyCollection<AdjacencyKind> kinds, bool seatLevelSide, CancellationToken cancellationToken)
    {
        var seats = seatGenerator.Generate(layout);
        var neighbours = Neighbours(adjacencyBuilder.Build(layout, seats, kinds, seatLevelSide), seats.Count);
        var taken = new bool[seats.Count];
        var size = 0;
        for (var i = 0; i < seats.Count; i++)
        {
            if (!neighbours[i].Any(n => taken[n]))
            {
                taken[i] = true;
                size++;
            }
        }

        return size;
    }
}