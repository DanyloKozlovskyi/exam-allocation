using Solver.Layout;
using Solver.Model;

namespace Solver.Allocation;

/// <summary>Per-call cache of α_r(kinds) by (layout hash, kinds, seat-level flag) (04 §3.2).</summary>
internal sealed class AlphaCache(IIndependentSetCalculator calculator)
{
    private const string AccessibleKeySuffix = "#accessible";
    private readonly Dictionary<string, int> values = new(StringComparer.Ordinal);

    public int Get(RoomFacts room, NoAdjacentByAttributeParameters parameters, CancellationToken cancellationToken)
    {
        var kinds = parameters.Adjacency.Distinct().Order().ToList();
        var key = room.LayoutKey + "#" + string.Join(',', kinds) + "#" + parameters.SeatLevelSide;
        lock (values)
        {
            if (values.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        var alpha = room.Capacity == 0 ? 0 : Math.Min(room.Capacity, calculator.Calculate(room.Room.Layout, kinds, parameters.SeatLevelSide, cancellationToken));
        lock (values)
        {
            values[key] = alpha;
        }

        return alpha;
    }

    /// <summary>
    /// α over the room's accessible seats only: participants who need an accessible seat can use nothing else,
    /// so same-value accessible participants are bounded by this (04 §4, accessibility-aware adjacency bound).
    /// </summary>
    public int GetAccessible(RoomFacts room, NoAdjacentByAttributeParameters parameters, CancellationToken cancellationToken)
    {
        var kinds = parameters.Adjacency.Distinct().Order().ToList();
        var key = room.LayoutKey + "#" + string.Join(',', kinds) + "#" + parameters.SeatLevelSide + AccessibleKeySuffix;
        lock (values)
        {
            if (values.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        var layout = AccessibleOnly(room.Room.Layout);
        var accessibleSeats = CountEnabled(layout);
        var alpha = accessibleSeats == 0 ? 0 : Math.Min(accessibleSeats, calculator.Calculate(layout, kinds, parameters.SeatLevelSide, cancellationToken));
        lock (values)
        {
            values[key] = alpha;
        }

        return alpha;
    }

    private static RoomLayoutModel AccessibleOnly(RoomLayoutModel layout)
    {
        var disabled = new HashSet<string>(layout.DisabledSeats, StringComparer.Ordinal);
        foreach (var key in AllSeatKeys(layout))
        {
            if (!layout.AccessibleSeats.Contains(key))
            {
                disabled.Add(key);
            }
        }

        return layout with { DisabledSeats = disabled };
    }

    private static int CountEnabled(RoomLayoutModel layout) => AllSeatKeys(layout).Count(key => !layout.DisabledSeats.Contains(key));

    private static IEnumerable<string> AllSeatKeys(RoomLayoutModel layout)
    {
        for (var row = 0; row < layout.Rows; row++)
        {
            for (var column = 0; column < layout.Columns; column++)
            {
                for (var seat = 0; seat < layout.SeatsPerDesk; seat++)
                {
                    yield return SeatKeyFormat.Format(row, column, seat);
                }
            }
        }
    }
}