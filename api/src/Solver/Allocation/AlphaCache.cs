using Solver.Layout;
using Solver.Model;

namespace Solver.Allocation;

/// <summary>Per-call cache of α_r(kinds) by (layout hash, kinds, seat-level flag) (04 §3.2).</summary>
internal sealed class AlphaCache(IIndependentSetCalculator calculator)
{
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
}