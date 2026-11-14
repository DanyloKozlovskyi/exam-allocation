using Solver.Model;

namespace Solver.Layout;

/// <summary>
/// Generates the enabled seats of a layout in seat-key order (row, column, seat).
/// The position of a seat in the returned list is its seat index used by adjacency edges.
/// </summary>
public sealed class SeatGenerator : ISeatGenerator
{
    public IReadOnlyList<SolverSeat> Generate(RoomLayoutModel layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var seats = new List<SolverSeat>(Math.Max(0, layout.Rows * layout.Columns * layout.SeatsPerDesk));
        for (var row = 0; row < layout.Rows; row++)
        {
            for (var column = 0; column < layout.Columns; column++)
            {
                for (var seat = 0; seat < layout.SeatsPerDesk; seat++)
                {
                    var key = SeatKeyFormat.Format(row, column, seat);
                    if (layout.DisabledSeats.Contains(key))
                    {
                        continue;
                    }

                    seats.Add(new SolverSeat(key, row, column, seat, layout.AccessibleSeats.Contains(key)));
                }
            }
        }

        return seats;
    }
}