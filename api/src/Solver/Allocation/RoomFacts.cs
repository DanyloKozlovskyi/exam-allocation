using System.Globalization;
using Solver.Layout;
using Solver.Model;

namespace Solver.Allocation;

/// <summary>Precomputed per-room facts (04 §3.2): enabled seats, capacities and a layout hash.</summary>
internal sealed record RoomFacts(
    int Index,
    SolverRoom Room,
    IReadOnlyList<SolverSeat> Seats,
    IReadOnlySet<string> SeatKeys,
    IReadOnlySet<string> AccessibleKeys,
    string LayoutKey)
{
    public int Capacity => SeatKeys.Count;

    public int AccessibleCapacity => AccessibleKeys.Count;

    public static IReadOnlyList<RoomFacts> Build(IReadOnlyList<SolverRoom> rooms, ISeatGenerator seatGenerator)
    {
        var seatsByLayout = new Dictionary<string, IReadOnlyList<SolverSeat>>(StringComparer.Ordinal);
        var result = new List<RoomFacts>(rooms.Count);
        for (var i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            var layoutKey = LayoutHash(room.Layout);
            if (!seatsByLayout.TryGetValue(layoutKey, out var seats))
            {
                seats = seatGenerator.Generate(room.Layout)
                    .Where(s => !room.Layout.DisabledSeats.Contains(s.Key))
                    .ToList();
                seatsByLayout[layoutKey] = seats;
            }

            var keys = seats.Select(s => s.Key).ToHashSet(StringComparer.Ordinal);
            var accessible = seats.Where(s => s.IsAccessible).Select(s => s.Key).ToHashSet(StringComparer.Ordinal);
            result.Add(new RoomFacts(i, room, seats, keys, accessible, layoutKey));
        }

        return result;
    }

    public static string LayoutHash(RoomLayoutModel layout)
        => string.Join(
            '|',
            layout.Rows.ToString(CultureInfo.InvariantCulture),
            layout.Columns.ToString(CultureInfo.InvariantCulture),
            layout.SeatsPerDesk.ToString(CultureInfo.InvariantCulture),
            string.Join(',', layout.AislesAfterColumns.Order().Select(a => a.ToString(CultureInfo.InvariantCulture))),
            string.Join(',', layout.DisabledSeats.Order(StringComparer.Ordinal)),
            string.Join(',', layout.AccessibleSeats.Order(StringComparer.Ordinal)));
}