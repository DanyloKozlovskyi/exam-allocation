using Solver.Model;

namespace Solver.Allocation;

/// <summary>Pinned participants as constants of stage 1 (04 §3.1): pin_r, pinAcc_r, pin_{a,v,r}.</summary>
internal sealed class PinFacts
{
    private readonly Dictionary<Guid, int> countByRoom = [];
    private readonly Dictionary<Guid, int> accessibleByRoom = [];
    private readonly Dictionary<(string Key, string Value, Guid RoomId), int> valueByRoom = [];
    private readonly Dictionary<(string Key, string Value, Guid RoomId), int> accessibleValueByRoom = [];

    private PinFacts(IReadOnlyList<(SolverPin Pin, SolverParticipant Participant)> pinned)
    {
        Pinned = pinned;
    }

    /// <summary>Pins whose participant exists, deduplicated by participant (first pin wins).</summary>
    public IReadOnlyList<(SolverPin Pin, SolverParticipant Participant)> Pinned { get; }

    public static PinFacts Build(SolverInput input, IReadOnlyList<string> referencedKeys)
    {
        var participants = input.Participants.ToDictionary(p => p.ParticipantId);
        var pinned = input.Pins
            .Where(p => participants.ContainsKey(p.ParticipantId))
            .DistinctBy(p => p.ParticipantId)
            .Select(p => (p, participants[p.ParticipantId]))
            .ToList();
        var facts = new PinFacts(pinned);
        foreach (var (pin, participant) in pinned)
        {
            facts.countByRoom[pin.RoomId] = facts.Count(pin.RoomId) + 1;
            if (participant.NeedsAccessibleSeat)
            {
                facts.accessibleByRoom[pin.RoomId] = facts.Accessible(pin.RoomId) + 1;
            }

            foreach (var key in referencedKeys)
            {
                if (AttributeValues.TryGet(participant, key, out var value))
                {
                    var cell = (key, value, pin.RoomId);
                    facts.valueByRoom[cell] = facts.valueByRoom.GetValueOrDefault(cell) + 1;
                    if (participant.NeedsAccessibleSeat)
                    {
                        facts.accessibleValueByRoom[cell] = facts.accessibleValueByRoom.GetValueOrDefault(cell) + 1;
                    }
                }
            }
        }

        return facts;
    }

    public int Count(Guid roomId) => countByRoom.GetValueOrDefault(roomId);

    public int Accessible(Guid roomId) => accessibleByRoom.GetValueOrDefault(roomId);

    public int Value(string key, string value, Guid roomId) => valueByRoom.GetValueOrDefault((key, value, roomId));

    /// <summary>pinAcc_{a,v,r}: pinned needs-accessible participants with value <paramref name="value"/> in a room.</summary>
    public int AccessibleValue(string key, string value, Guid roomId) => accessibleValueByRoom.GetValueOrDefault((key, value, roomId));

    public int Value(string key, string value, IEnumerable<Guid> roomIds) => roomIds.Sum(r => Value(key, value, r));

    /// <summary>Total pinned participants per value of <paramref name="key"/>.</summary>
    public IReadOnlyDictionary<string, int> ValueTotals(string key)
        => valueByRoom
            .Where(e => e.Key.Key == key)
            .GroupBy(e => e.Key.Value, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Value), StringComparer.Ordinal);
}