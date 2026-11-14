using Solver.Layout;
using Solver.Model;

namespace Solver.Seating;

/// <summary>Per-call state of one stage-2 room solve (seats, units, stage-2 rules, memoized edges).</summary>
internal sealed class SeatingContext
{
    private readonly IAdjacencyBuilder _adjacencyBuilder;
    private readonly Dictionary<string, IReadOnlyList<SeatEdge>> _edges = new(StringComparer.Ordinal);

    public SeatingContext(
        SeatingRequest request,
        IReadOnlyList<SolverSeat> seats,
        IReadOnlyList<SeatingUnit> units,
        IReadOnlyList<IReadOnlyList<int>> allowedSeats,
        IAdjacencyBuilder adjacencyBuilder)
    {
        Request = request;
        Seats = seats;
        Units = units;
        AllowedSeats = allowedSeats;
        _adjacencyBuilder = adjacencyBuilder;
        AdjacencyRules = request.Rules
            .Where(r => r.Type == RuleType.NoAdjacentByAttribute)
            .Select(r => r.Parameters is NoAdjacentByAttributeParameters p ? new AdjacencyRule(r, p) : null)
            .OfType<AdjacencyRule>()
            .ToList();
        SpreadRules = request.Rules
            .Where(r => r.Type == RuleType.SpreadInRoom)
            .Select(r => r.Parameters is SpreadInRoomParameters p ? new SpreadRule(r, p) : null)
            .OfType<SpreadRule>()
            .ToList();
        FillRules = request.Rules.Where(r => r.Type == RuleType.FillFromFront).ToList();
    }

    public SeatingRequest Request { get; }

    public IReadOnlyList<SolverSeat> Seats { get; }

    public IReadOnlyList<SeatingUnit> Units { get; }

    public IReadOnlyList<IReadOnlyList<int>> AllowedSeats { get; }

    public IReadOnlyList<AdjacencyRule> AdjacencyRules { get; }

    public IReadOnlyList<SpreadRule> SpreadRules { get; }

    public IReadOnlyList<SolverRule> FillRules { get; }

    public IReadOnlyList<SeatEdge> Edges(IReadOnlyList<AdjacencyKind> kinds, bool seatLevelSide)
    {
        var key = string.Join(',', kinds.Distinct().Order()) + seatLevelSide;
        if (!_edges.TryGetValue(key, out var edges))
        {
            edges = _adjacencyBuilder.Build(Request.Room.Layout, Seats, kinds, seatLevelSide);
            _edges[key] = edges;
        }

        return edges;
    }

    /// <summary>Unit index for a previous assignment (class or pinned participant), or -1.</summary>
    public int FindUnit(ClassSeatAssignment assignment)
    {
        for (var u = 0; u < Units.Count; u++)
        {
            var unit = Units[u];
            if ((assignment.PinnedParticipantId is { } pinned && unit.PinnedParticipantId == pinned)
                || (assignment.PinnedParticipantId is null && assignment.ClassIndex is { } classIndex && unit.ClassIndex == classIndex))
            {
                return u;
            }
        }

        return -1;
    }
}

internal sealed record AdjacencyRule(SolverRule Rule, NoAdjacentByAttributeParameters Parameters);

internal sealed record SpreadRule(SolverRule Rule, SpreadInRoomParameters Parameters);