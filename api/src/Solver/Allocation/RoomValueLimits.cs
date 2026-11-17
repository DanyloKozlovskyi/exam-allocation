using Solver.Model;

namespace Solver.Allocation;

/// <summary>
/// Hard per-room limits on the members of one value of an attribute key (04 §4.1 ℓ_τ(a), ℓ^acc_τ(a)):
/// hard Room-scoped R2 maxima and hard R3 α-bounds; shared by room selection and the greedy assignment.
/// </summary>
internal sealed class RoomValueLimits
{
    private readonly int? separation;
    private readonly IReadOnlyList<NoAdjacentByAttributeParameters> adjacency;
    private readonly AlphaCache alpha;
    private readonly CancellationToken cancellationToken;

    private RoomValueLimits(string key, int? separation, IReadOnlyList<NoAdjacentByAttributeParameters> adjacency, AlphaCache alpha, CancellationToken cancellationToken)
    {
        Key = key;
        this.separation = separation;
        this.adjacency = adjacency;
        this.alpha = alpha;
        this.cancellationToken = cancellationToken;
    }

    public string Key { get; }

    public static IReadOnlyList<RoomValueLimits> Build(Stage1Context context, CancellationToken cancellationToken)
    {
        var hard = context.Input.Rules.Where(r => r.IsHard).ToList();
        var separations = hard
            .Select(r => r.Parameters)
            .OfType<SeparateByAttributeParameters>()
            .Where(p => p.Scope == RuleScope.Room)
            .GroupBy(p => p.AttributeKey, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Min(p => p.MaxPerScope), StringComparer.Ordinal);
        var adjacencies = hard
            .Select(r => r.Parameters)
            .OfType<NoAdjacentByAttributeParameters>()
            .GroupBy(p => p.AttributeKey, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<NoAdjacentByAttributeParameters>)g.ToList(), StringComparer.Ordinal);
        return separations.Keys
            .Concat(adjacencies.Keys)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(key => new RoomValueLimits(
                key,
                separations.TryGetValue(key, out var m) ? m : null,
                adjacencies.GetValueOrDefault(key) ?? [],
                context.Alpha,
                cancellationToken))
            .ToList();
    }

    /// <summary>ℓ_r(a): at most this many members of one value fit room <paramref name="room"/>.</summary>
    public int PerRoom(RoomFacts room)
    {
        var limit = Math.Min(room.Capacity, separation ?? int.MaxValue);
        foreach (var parameters in adjacency)
        {
            limit = Math.Min(limit, alpha.Get(room, parameters, cancellationToken));
        }

        return Math.Max(0, limit);
    }

    /// <summary>ℓ^acc_r(a): at most this many needs-accessible members of one value fit room <paramref name="room"/>.</summary>
    public int PerRoomAccessible(RoomFacts room)
    {
        var limit = Math.Min(room.AccessibleCapacity, separation ?? int.MaxValue);
        foreach (var parameters in adjacency)
        {
            limit = Math.Min(limit, alpha.GetAccessible(room, parameters, cancellationToken));
        }

        return Math.Max(0, limit);
    }
}