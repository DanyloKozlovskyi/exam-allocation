using Solver.Model;

namespace Solver.Layout;

/// <summary>
/// Builds undirected seat-adjacency edges (docs/03 "Seat adjacency").
/// <see cref="AdjacencyKind.SameDesk"/> is always on; disabled seats are absent from <c>seats</c>
/// and therefore never appear in an edge. Edges are seat-index pairs with A &lt; B, sorted.
/// </summary>
public sealed class AdjacencyBuilder : IAdjacencyBuilder
{
    private const int NeighbourOffset = 1;

    public IReadOnlyList<SeatEdge> Build(RoomLayoutModel layout, IReadOnlyList<SolverSeat> seats, IReadOnlyCollection<AdjacencyKind> kinds, bool seatLevelSide)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(seats);
        ArgumentNullException.ThrowIfNull(kinds);

        var side = kinds.Contains(AdjacencyKind.Side);
        var sideAcrossAisle = kinds.Contains(AdjacencyKind.SideAcrossAisle);
        var frontBack = kinds.Contains(AdjacencyKind.FrontBack);
        var diagonal = kinds.Contains(AdjacencyKind.Diagonal);
        var aisles = layout.AislesAfterColumns.ToHashSet();
        var lastSeat = layout.SeatsPerDesk - NeighbourOffset;

        // Seats grouped by desk (row, column) → seat indices.
        var desks = new Dictionary<(int Row, int Column), List<(int Index, int Seat)>>();
        for (var index = 0; index < seats.Count; index++)
        {
            var seat = seats[index];
            var desk = (seat.Row, seat.Column);
            if (!desks.TryGetValue(desk, out var members))
            {
                members = [];
                desks[desk] = members;
            }

            members.Add((index, seat.SeatIndex));
        }

        var edges = new HashSet<SeatEdge>();

        void Connect(int a, int b)
        {
            if (a != b)
            {
                edges.Add(a < b ? new SeatEdge(a, b) : new SeatEdge(b, a));
            }
        }

        void ConnectDesks((int Row, int Column) first, (int Row, int Column) second, Func<int, int, bool> seatFilter)
        {
            if (!desks.TryGetValue(first, out var left) || !desks.TryGetValue(second, out var right))
            {
                return;
            }

            foreach (var a in left)
            {
                foreach (var b in right)
                {
                    if (seatFilter(a.Seat, b.Seat))
                    {
                        Connect(a.Index, b.Index);
                    }
                }
            }
        }

        static bool AnySeats(int left, int right) => true;
        bool FacingSeats(int left, int right) => left == lastSeat && right == 0;

        foreach (var ((row, column), members) in desks)
        {
            // SameDesk: always on.
            for (var i = 0; i < members.Count; i++)
            {
                for (var j = i + 1; j < members.Count; j++)
                {
                    Connect(members[i].Index, members[j].Index);
                }
            }

            var rightColumn = column + NeighbourOffset;
            var aisleToRight = aisles.Contains(column);
            if ((side && !aisleToRight) || (sideAcrossAisle && aisleToRight))
            {
                var filter = !aisleToRight && seatLevelSide ? FacingSeats : (Func<int, int, bool>)AnySeats;
                ConnectDesks((row, column), (row, rightColumn), filter);
            }

            var nextRow = row + NeighbourOffset;
            if (frontBack)
            {
                ConnectDesks((row, column), (nextRow, column), AnySeats);
            }

            if (diagonal)
            {
                if (!aisleToRight)
                {
                    ConnectDesks((row, column), (nextRow, rightColumn), AnySeats);
                }

                var leftColumn = column - NeighbourOffset;
                if (!aisles.Contains(leftColumn))
                {
                    ConnectDesks((row, column), (nextRow, leftColumn), AnySeats);
                }
            }
        }

        return edges.OrderBy(e => e.A).ThenBy(e => e.B).ToList();
    }
}