using Solver.Layout;
using Solver.Model;

namespace Solver.Tests.Seating;

/// <summary>Builds stage-2 requests and checks seatings directly (adjacency/occupancy) for WS-B tests.</summary>
public sealed class SeatingRequestFactory
{
    public const string GroupKey = TestInputBuilder.GroupKey;
    public const int TimeLimitSeconds = 10;

    private readonly List<ParticipantClass> _classes = [];
    private readonly List<RoomClassCount> _counts = [];
    private readonly List<SolverRule> _rules = [];
    private readonly List<SolverPin> _pins = [];
    private readonly List<SolverParticipant> _pinned = [];
    private readonly List<IReadOnlyList<ClassSeatAssignment>> _previous = [];

    public SeatingRequestFactory(RoomLayoutModel layout)
    {
        Room = new SolverRoom(Guid.NewGuid(), Guid.NewGuid(), 1m, layout);
    }

    public SolverRoom Room { get; }

    public IReadOnlyList<ParticipantClass> Classes => _classes;

    public SeatingRequestFactory WithClass(string group, int count, bool needsAccessibleSeat = false)
    {
        var index = _classes.Count;
        _classes.Add(new ParticipantClass(
            index,
            new Dictionary<string, string> { [GroupKey] = group },
            needsAccessibleSeat,
            Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList()));
        _counts.Add(new RoomClassCount(index, Room.RoomId, count));
        return this;
    }

    public SeatingRequestFactory WithRule(RuleType type, RuleParameters parameters, bool isHard = true, int weight = 10)
    {
        _rules.Add(new SolverRule(Guid.NewGuid(), type, isHard, weight, parameters));
        return this;
    }

    public SeatingRequestFactory WithNoAdjacent(bool isHard, params AdjacencyKind[] kinds)
        => WithRule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(GroupKey, kinds, false), isHard);

    public Guid WithPin(string group, string? seatKey, bool needsAccessibleSeat = false)
    {
        var id = Guid.NewGuid();
        _pins.Add(new SolverPin(id, Room.RoomId, seatKey));
        _pinned.Add(new SolverParticipant(id, new Dictionary<string, string> { [GroupKey] = group }, needsAccessibleSeat, group));
        return id;
    }

    public SeatingRequestFactory WithPrevious(IReadOnlyList<ClassSeatAssignment> previous)
    {
        _previous.Add(previous);
        return this;
    }

    public SeatingRequest Build(int seed = SolverDefaults.RandomSeed)
        => new(Room, _classes.ToList(), _counts.ToList(), _rules.ToList(), _pins.ToList(), _pinned.ToList(), seed, TimeLimitSeconds, _previous.ToList());

    /// <summary>Group of the occupant of each seat key.</summary>
    public Dictionary<string, string> GroupBySeat(SeatingResult result)
        => result.Assignments.ToDictionary(
            a => a.SeatKey,
            a => a.ClassIndex is { } k
                ? _classes[k].Values[GroupKey]
                : _pinned.Single(p => p.ParticipantId == a.PinnedParticipantId).Attributes[GroupKey]);

    /// <summary>Same-group pairs on adjacency edges of the given kinds (SameDesk always on).</summary>
    public int SameGroupAdjacentPairs(SeatingResult result, params AdjacencyKind[] kinds)
    {
        var seats = new SeatGenerator().Generate(Room.Layout);
        var edges = new AdjacencyBuilder().Build(Room.Layout, seats, kinds, false);
        var groups = GroupBySeat(result);
        return edges.Count(e => groups.TryGetValue(seats[e.A].Key, out var a) && groups.TryGetValue(seats[e.B].Key, out var b) && a == b);
    }

    public int OccupiedAdjacentPairs(SeatingResult result, params AdjacencyKind[] kinds)
    {
        var seats = new SeatGenerator().Generate(Room.Layout);
        var edges = new AdjacencyBuilder().Build(Room.Layout, seats, kinds, false);
        var occupied = result.Assignments.Select(a => a.SeatKey).ToHashSet();
        return edges.Count(e => occupied.Contains(seats[e.A].Key) && occupied.Contains(seats[e.B].Key));
    }
}