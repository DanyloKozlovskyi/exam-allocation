using Solver.Model;

namespace Solver.Allocation;

/// <summary>Greedy class-to-room assignment inside the selected rooms (04 §4.1), used as a complete stage-1 hint.</summary>
internal sealed record GreedyAssignmentResult(IReadOnlyList<RoomClassCount> Counts, bool Complete);

/// <summary>
/// Places every class member into the fitting selected room with the most free seats, respecting capacity,
/// accessibility, hard R2 (room and center scope), hard R3 α-bounds and feedback cuts (04 §4.1 "Greedy assignment").
/// </summary>
internal sealed class GreedyAssignment
{
    private readonly Stage1Context context;
    private readonly IReadOnlyList<RoomFacts> rooms;
    private readonly IReadOnlyList<RoomValueLimits> roomLimits;
    private readonly IReadOnlyList<(string Key, int Max)> centerLimits;
    private readonly IReadOnlyList<Stage1Cut> cuts;
    private readonly int[] free;
    private readonly int[] freeAccessible;
    private readonly Dictionary<(string Key, string Value, int Room), int> roomCounts = [];
    private readonly Dictionary<(string Key, string Value, int Room), int> roomAccessibleCounts = [];
    private readonly Dictionary<(string Key, string Value, Guid Center), int> centerCounts = [];
    private readonly Dictionary<(int Room, int Limit), (int PerRoom, int PerRoomAccessible)> limitCache = [];
    private readonly IReadOnlyList<string> keys;

    private GreedyAssignment(Stage1Context context, IReadOnlySet<int> selected, IReadOnlyList<Stage1Cut> cuts, CancellationToken cancellationToken)
    {
        this.context = context;
        this.cuts = cuts;
        rooms = context.Rooms.Where(r => selected.Contains(r.Index)).OrderBy(r => r.Index).ToList();
        roomLimits = RoomValueLimits.Build(context, cancellationToken);
        centerLimits = context.Input.Rules
            .Where(r => r.IsHard)
            .Select(r => r.Parameters)
            .OfType<SeparateByAttributeParameters>()
            .Where(p => p.Scope == RuleScope.Center)
            .Select(p => (p.AttributeKey, p.MaxPerScope))
            .ToList();
        keys = roomLimits.Select(l => l.Key)
            .Concat(centerLimits.Select(l => l.Key))
            .Concat(cuts.Select(c => c.AttributeKey))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        free = rooms.Select(r => r.Capacity - context.Pins.Count(r.Room.RoomId)).ToArray();
        freeAccessible = rooms.Select(r => r.AccessibleCapacity - context.Pins.Accessible(r.Room.RoomId)).ToArray();
        SeedPins();
    }

    public static GreedyAssignmentResult Assign(Stage1Context context, IReadOnlySet<int> selected, IReadOnlyList<Stage1Cut> cuts, CancellationToken cancellationToken)
        => new GreedyAssignment(context, selected, cuts, cancellationToken).Run(cancellationToken);

    private GreedyAssignmentResult Run(CancellationToken cancellationToken)
    {
        var counts = new Dictionary<(int ClassIndex, int Room), int>();
        var complete = true;
        var allowed = new int[rooms.Count];
        foreach (var participantClass in Order())
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var r = 0; r < rooms.Count; r++)
            {
                allowed[r] = Allowance(participantClass, r);
            }

            for (var member = 0; member < participantClass.Size; member++)
            {
                var best = -1;
                for (var r = 0; r < rooms.Count; r++)
                {
                    if (allowed[r] > 0 && (best < 0 || Better(participantClass, r, best)))
                    {
                        best = r;
                    }
                }

                if (best < 0)
                {
                    complete = false;
                    break;
                }

                allowed[best]--;
                Place(participantClass, best);
                if (centerLimits.Count > 0)
                {
                    // A center-scoped R2 limit is shared by every room of the center: refresh their allowances.
                    var center = rooms[best].Room.CenterId;
                    for (var r = 0; r < rooms.Count; r++)
                    {
                        if (rooms[r].Room.CenterId == center)
                        {
                            allowed[r] = Allowance(participantClass, r);
                        }
                    }
                }

                var cell = (participantClass.Index, best);
                counts[cell] = counts.GetValueOrDefault(cell) + 1;
            }
        }

        var result = counts
            .OrderBy(c => rooms[c.Key.Room].Index)
            .ThenBy(c => c.Key.ClassIndex)
            .Select(c => new RoomClassCount(c.Key.ClassIndex, rooms[c.Key.Room].Room.RoomId, c.Value))
            .ToList();
        return new GreedyAssignmentResult(result, complete);
    }

    /// <summary>Needs-accessible classes first, then by descending size of their largest limited value.</summary>
    private IEnumerable<ParticipantClass> Order()
    {
        var limitedKeys = keys;
        var valueSizes = limitedKeys.ToDictionary(
            key => key,
            key => context.ClassesByValue(key).ToDictionary(e => e.Key, e => e.Value.Sum(c => c.Size), StringComparer.Ordinal),
            StringComparer.Ordinal);
        return context.Classes
            .OrderByDescending(c => c.NeedsAccessibleSeat)
            .ThenByDescending(c => limitedKeys
                .Select(key => AttributeValues.TryGet(c, key, out var value) ? valueSizes[key].GetValueOrDefault(value) : 0)
                .DefaultIfEmpty(c.Size)
                .Max())
            .ThenBy(c => c.Index);
    }

    private bool Better(ParticipantClass participantClass, int candidate, int best)
    {
        if (participantClass.NeedsAccessibleSeat && freeAccessible[candidate] != freeAccessible[best])
        {
            return freeAccessible[candidate] > freeAccessible[best];
        }

        return free[candidate] > free[best];
    }

    /// <summary>How many more members of the class fit room <paramref name="r"/> under every limit.</summary>
    private int Allowance(ParticipantClass participantClass, int r)
    {
        var room = rooms[r];
        var allowance = free[r];
        if (participantClass.NeedsAccessibleSeat)
        {
            allowance = Math.Min(allowance, freeAccessible[r]);
        }

        for (var l = 0; l < roomLimits.Count && allowance > 0; l++)
        {
            var limit = roomLimits[l];
            if (!AttributeValues.TryGet(participantClass, limit.Key, out var value))
            {
                continue;
            }

            var (perRoom, perRoomAccessible) = Limits(r, l);
            allowance = Math.Min(allowance, perRoom - roomCounts.GetValueOrDefault((limit.Key, value, r)));
            if (participantClass.NeedsAccessibleSeat)
            {
                allowance = Math.Min(allowance, perRoomAccessible - roomAccessibleCounts.GetValueOrDefault((limit.Key, value, r)));
            }
        }

        foreach (var (key, max) in centerLimits)
        {
            if (AttributeValues.TryGet(participantClass, key, out var value))
            {
                allowance = Math.Min(allowance, max - centerCounts.GetValueOrDefault((key, value, room.Room.CenterId)));
            }
        }

        foreach (var cut in cuts.Where(c => c.RoomId == room.Room.RoomId))
        {
            if (AttributeValues.TryGet(participantClass, cut.AttributeKey, out var value) && value == cut.Value)
            {
                allowance = Math.Min(allowance, cut.MaxCount - roomCounts.GetValueOrDefault((cut.AttributeKey, value, r)));
            }
        }

        return Math.Max(0, allowance);
    }

    private (int PerRoom, int PerRoomAccessible) Limits(int r, int l)
    {
        if (!limitCache.TryGetValue((r, l), out var limits))
        {
            limits = (roomLimits[l].PerRoom(rooms[r]), roomLimits[l].PerRoomAccessible(rooms[r]));
            limitCache[(r, l)] = limits;
        }

        return limits;
    }

    private void Place(ParticipantClass participantClass, int r)
    {
        free[r]--;
        if (participantClass.NeedsAccessibleSeat)
        {
            freeAccessible[r]--;
        }

        foreach (var key in keys)
        {
            if (AttributeValues.TryGet(participantClass, key, out var value))
            {
                Count(key, value, r, participantClass.NeedsAccessibleSeat);
            }
        }
    }

    private void SeedPins()
    {
        var indexById = rooms.Select((room, position) => (room.Room.RoomId, position)).ToDictionary(e => e.RoomId, e => e.position);
        foreach (var (pin, participant) in context.Pins.Pinned)
        {
            if (!indexById.TryGetValue(pin.RoomId, out var r))
            {
                continue;
            }

            foreach (var key in keys)
            {
                if (AttributeValues.TryGet(participant, key, out var value))
                {
                    Count(key, value, r, participant.NeedsAccessibleSeat);
                }
            }
        }
    }

    private void Count(string key, string value, int r, bool accessible)
    {
        roomCounts[(key, value, r)] = roomCounts.GetValueOrDefault((key, value, r)) + 1;
        if (accessible)
        {
            roomAccessibleCounts[(key, value, r)] = roomAccessibleCounts.GetValueOrDefault((key, value, r)) + 1;
        }

        var center = rooms[r].Room.CenterId;
        centerCounts[(key, value, center)] = centerCounts.GetValueOrDefault((key, value, center)) + 1;
    }
}