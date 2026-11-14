using Solver.Layout;
using Solver.Model;

namespace Solver.Allocation;

/// <summary>Everything stage 1 needs besides the CP model: room facts, pins, classes per value.</summary>
internal sealed class Stage1Context
{
    private readonly Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<ParticipantClass>>> classesByValue = new(StringComparer.Ordinal);

    private Stage1Context(SolverInput input, IReadOnlyList<ParticipantClass> classes, IReadOnlyList<RoomFacts> rooms, PinFacts pins, AlphaCache alpha)
    {
        Input = input;
        Classes = classes;
        Rooms = rooms;
        Pins = pins;
        Alpha = alpha;
        RoomIndexById = rooms.ToDictionary(r => r.Room.RoomId, r => r.Index);
    }

    public SolverInput Input { get; }

    public IReadOnlyList<ParticipantClass> Classes { get; }

    public IReadOnlyList<RoomFacts> Rooms { get; }

    public IReadOnlyDictionary<Guid, int> RoomIndexById { get; }

    public PinFacts Pins { get; }

    public AlphaCache Alpha { get; }

    public static Stage1Context Create(SolverInput input, IReadOnlyList<ParticipantClass> classes, ISeatGenerator seatGenerator, IIndependentSetCalculator calculator)
        => new(input, classes, RoomFacts.Build(input.Rooms, seatGenerator), PinFacts.Build(input, AttributeValues.ReferencedKeys(input.Rules)), new AlphaCache(calculator));

    /// <summary>Classes grouped by their value of <paramref name="key"/>; classes with a missing value are omitted.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<ParticipantClass>> ClassesByValue(string key)
    {
        if (!classesByValue.TryGetValue(key, out var map))
        {
            map = Classes
                .Where(c => AttributeValues.TryGet(c, key, out _))
                .GroupBy(c => c.Values[key], StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ParticipantClass>)g.ToList(), StringComparer.Ordinal);
            classesByValue[key] = map;
        }

        return map;
    }

    /// <summary>Values of <paramref name="key"/> present among classes or pins.</summary>
    public IReadOnlyList<string> Values(string key)
        => ClassesByValue(key).Keys.Concat(Pins.ValueTotals(key).Keys).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

    public IReadOnlyList<IReadOnlyList<RoomFacts>> Units(RuleScope scope)
        => scope == RuleScope.Room
            ? Rooms.Select(r => (IReadOnlyList<RoomFacts>)[r]).ToList()
            : Rooms.GroupBy(r => r.Room.CenterId).Select(g => (IReadOnlyList<RoomFacts>)g.ToList()).ToList();

    public int MovableParticipants => Classes.Sum(c => c.Size);
}