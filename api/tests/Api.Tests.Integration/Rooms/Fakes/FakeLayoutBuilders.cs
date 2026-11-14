using Solver.Layout;
using Solver.Model;

namespace Api.Tests.Integration.Rooms.Fakes;

/// <summary>Simple grid seat generator used until WS-B's SeatGenerator is merged.</summary>
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

/// <summary>Adjacency builder fake: only same-desk edges.</summary>
public sealed class FakeAdjacencyBuilder : IAdjacencyBuilder
{
    public IReadOnlyList<SeatEdge> Build(RoomLayoutModel layout, IReadOnlyList<SolverSeat> seats, IReadOnlyCollection<AdjacencyKind> kinds, bool seatLevelSide)
    {
        if (!kinds.Contains(AdjacencyKind.SameDesk))
        {
            return [];
        }

        var edges = new List<SeatEdge>();
        for (var a = 0; a < seats.Count; a++)
        {
            for (var b = a + 1; b < seats.Count; b++)
            {
                if (seats[a].Row == seats[b].Row && seats[a].Column == seats[b].Column)
                {
                    edges.Add(new SeatEdge(a, b));
                }
            }
        }

        return edges;
    }
}