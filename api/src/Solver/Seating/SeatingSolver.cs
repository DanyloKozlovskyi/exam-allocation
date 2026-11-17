using Google.OrTools.Sat;
using Solver.Layout;
using Solver.Model;

namespace Solver.Seating;

/// <summary>
/// Stage 2 (docs/04 §5): seats the classes and pinned participants that stage 1 placed in one room.
/// Stateless and safe to call concurrently for different rooms; each CP-SAT model uses one worker.
/// </summary>
public sealed class SeatingSolver(ISeatGenerator seatGenerator, IAdjacencyBuilder adjacencyBuilder) : ISeatingSolver
{
    private const int Workers = 1;
    private const int MaxOccupantsPerSeat = 1;
    private const int MinCountForConflict = 2;
    private const int MinHammingChanges = 1;

    public SeatingSolver()
        : this(new SeatGenerator(), new AdjacencyBuilder())
    {
    }

    public SeatingResult Solve(SeatingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (cancellationToken.IsCancellationRequested)
        {
            return Empty(SolveStatus.Cancelled);
        }

        var seats = seatGenerator.Generate(request.Room.Layout);
        var units = BuildUnits(request, seats);
        if (units is null)
        {
            return Empty(SolveStatus.Infeasible);
        }

        var context = new SeatingContext(request, seats, units, BuildAllowedSeats(seats, units), adjacencyBuilder);
        if (!HasEnoughSeats(context))
        {
            return Empty(SolveStatus.Infeasible);
        }

        if (!NeedsModel(context))
        {
            var greedy = Greedy(context);
            return greedy is null ? Empty(SolveStatus.Infeasible) : Result(context, SolveStatus.Optimal, greedy);
        }

        var withDiversity = request.PreviousSeatings.Count > 0;
        var (status, occupant) = SolveModel(context, withDiversity, cancellationToken);
        if (withDiversity && status == SolveStatus.Infeasible)
        {
            // Seat-level diversity is best effort per room (docs/04 §8): fall back to an unconstrained seating.
            (status, occupant) = SolveModel(context, false, cancellationToken);
        }

        return occupant is null ? Empty(status) : Result(context, status, occupant);
    }

    private static SeatingResult Empty(SolveStatus status) => new(status, [], 0, new Dictionary<Guid, long>());

    private static List<SeatingUnit>? BuildUnits(SeatingRequest request, IReadOnlyList<SolverSeat> seats)
    {
        var roomId = request.Room.RoomId;
        var seatIndexByKey = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < seats.Count; i++)
        {
            seatIndexByKey[seats[i].Key] = i;
        }

        var participants = request.PinnedParticipants
            .GroupBy(p => p.ParticipantId)
            .ToDictionary(g => g.Key, g => g.First());
        var units = new List<SeatingUnit>();
        var fixedSeats = new HashSet<int>();

        foreach (var pin in request.Pins.Where(p => p.RoomId == roomId).DistinctBy(p => p.ParticipantId).OrderBy(p => p.ParticipantId))
        {
            participants.TryGetValue(pin.ParticipantId, out var participant);
            var values = participant?.Attributes ?? new Dictionary<string, string>();
            var needsAccessible = participant?.NeedsAccessibleSeat ?? false;
            int? fixedSeat = null;
            if (pin.SeatKey is not null)
            {
                if (!seatIndexByKey.TryGetValue(pin.SeatKey, out var seatIndex) || !fixedSeats.Add(seatIndex))
                {
                    return null;
                }

                fixedSeat = seatIndex;
            }

            units.Add(new SeatingUnit(null, pin.ParticipantId, 1, needsAccessible, values, fixedSeat));
        }

        var classes = request.Classes.ToDictionary(c => c.Index);
        var counts = request.Counts
            .Where(c => c.RoomId == roomId && c.Count > 0)
            .GroupBy(c => c.ClassIndex)
            .OrderBy(g => g.Key);
        foreach (var count in counts)
        {
            if (!classes.TryGetValue(count.Key, out var participantClass))
            {
                throw new ArgumentException($"Class {count.Key} is referenced by counts but missing from classes.", nameof(request));
            }

            units.Add(new SeatingUnit(count.Key, null, count.Sum(c => c.Count), participantClass.NeedsAccessibleSeat, participantClass.Values, null));
        }

        return units;
    }

    private static List<int>[] BuildAllowedSeats(IReadOnlyList<SolverSeat> seats, IReadOnlyList<SeatingUnit> units)
    {
        var fixedSeats = units.Where(u => u.FixedSeat is not null).Select(u => u.FixedSeat!.Value).ToHashSet();
        var allowed = new List<int>[units.Count];
        for (var u = 0; u < units.Count; u++)
        {
            var unit = units[u];
            allowed[u] = unit.FixedSeat is { } fixedSeat
                ? [fixedSeat]
                : Enumerable.Range(0, seats.Count)
                    .Where(p => !fixedSeats.Contains(p) && (!unit.NeedsAccessibleSeat || seats[p].IsAccessible))
                    .ToList();
        }

        return allowed;
    }

    private static bool HasEnoughSeats(SeatingContext context)
    {
        var fixedCount = context.Units.Count(u => !u.IsMovable);
        var free = context.Seats.Count - fixedCount;
        var movable = context.Units.Where(u => u.IsMovable).ToList();
        var freeAccessible = Enumerable.Range(0, context.Seats.Count)
            .Count(p => context.Seats[p].IsAccessible && context.Units.All(u => u.FixedSeat != p));
        return movable.Sum(u => u.Count) <= free
            && movable.Where(u => u.NeedsAccessibleSeat).Sum(u => u.Count) <= freeAccessible;
    }

    private static bool NeedsModel(SeatingContext context)
        => context.Request.PreviousSeatings.Count > 0
            || context.SpreadRules.Count > 0
            || context.FillRules.Count > 0
            || context.AdjacencyRules.Any(r => ConflictingValues(context, r.Parameters.AttributeKey).Count > 0);

    /// <summary>Values of an attribute held by at least two occupants of the room (only those can conflict).</summary>
    private static List<string> ConflictingValues(SeatingContext context, string attributeKey)
        => context.Units
            .Select(u => (Value: u.ValueOf(attributeKey), u.Count))
            .Where(x => x.Value is not null)
            .GroupBy(x => x.Value!, StringComparer.Ordinal)
            .Where(g => g.Sum(x => x.Count) >= MinCountForConflict)
            .Select(g => g.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Greedy seating in seat-key order: fixed pins first, then units that need accessible seats
    /// (accessible seats in key order), then the rest. Returns occupant unit per seat, or null if it does not fit.
    /// </summary>
    private static int[]? Greedy(SeatingContext context)
    {
        var occupant = Enumerable.Repeat(-1, context.Seats.Count).ToArray();
        var order = Enumerable.Range(0, context.Units.Count)
            .OrderBy(u => context.Units[u].IsMovable)
            .ThenBy(u => !context.Units[u].NeedsAccessibleSeat)
            .ThenBy(u => u);
        foreach (var u in order)
        {
            var remaining = context.Units[u].Count;
            foreach (var p in context.AllowedSeats[u])
            {
                if (remaining == 0)
                {
                    break;
                }

                if (occupant[p] < 0)
                {
                    occupant[p] = u;
                    remaining--;
                }
            }

            if (remaining > 0)
            {
                return null;
            }
        }

        return occupant;
    }

    private static (SolveStatus Status, int[]? Occupant) SolveModel(SeatingContext context, bool withDiversity, CancellationToken cancellationToken)
    {
        var model = new CpModel();
        var units = context.Units;
        var seatCount = context.Seats.Count;

        // u[k][p]: unit k sits on seat p.
        var vars = new Dictionary<int, BoolVar>[units.Count];
        var bySeat = new List<(int Unit, BoolVar Var)>[seatCount];
        for (var p = 0; p < seatCount; p++)
        {
            bySeat[p] = [];
        }

        for (var u = 0; u < units.Count; u++)
        {
            vars[u] = [];
            foreach (var p in context.AllowedSeats[u])
            {
                var v = model.NewBoolVar(string.Empty);
                vars[u][p] = v;
                bySeat[p].Add((u, v));
            }

            // Hard 1: Σ_p u[k,p] = n_k (fixed pins: their seat only, so forced to 1).
            model.Add(LinearExpr.Sum(vars[u].Values) == units[u].Count);
        }

        // Hard 2: a seat holds at most one occupant.
        for (var p = 0; p < seatCount; p++)
        {
            if (bySeat[p].Count > MaxOccupantsPerSeat)
            {
                model.AddAtMostOne(bySeat[p].Select(x => (ILiteral)x.Var));
            }
        }

        var objective = LinearExpr.NewBuilder();
        AddAdjacencyRules(context, model, bySeat, objective);
        AddSpreadRules(context, model, bySeat, objective);
        AddFillRules(context, vars, objective);
        if (withDiversity)
        {
            AddDiversity(context, model, vars);
        }

        model.Minimize(objective);

        var greedy = Greedy(context);
        if (greedy is not null)
        {
            for (var p = 0; p < seatCount; p++)
            {
                foreach (var (u, v) in bySeat[p])
                {
                    model.AddHint(v, greedy[p] == u);
                }
            }
        }

        var timeLimit = context.Request.TimeLimitSeconds > 0 ? context.Request.TimeLimitSeconds : SolverDefaults.Stage2TimeLimitSecondsPerRoom;
        using var solver = new CpSolver { StringParameters = CpSatParameters.Build(Workers, timeLimit, context.Request.RandomSeed) };

        CpSolverStatus cpStatus;
        using (cancellationToken.Register(solver.StopSearch))
        {
            cpStatus = solver.Solve(model);
        }

        var status = cpStatus switch
        {
            CpSolverStatus.Optimal => SolveStatus.Optimal,
            CpSolverStatus.Feasible => SolveStatus.Feasible,
            CpSolverStatus.Infeasible => SolveStatus.Infeasible,
            CpSolverStatus.Unknown when cancellationToken.IsCancellationRequested => SolveStatus.Cancelled,
            CpSolverStatus.Unknown => SolveStatus.NoSolutionInTime,
            _ => SolveStatus.Failed,
        };

        if (status is not (SolveStatus.Optimal or SolveStatus.Feasible))
        {
            return (status, null);
        }

        var occupant = Enumerable.Repeat(-1, seatCount).ToArray();
        for (var p = 0; p < seatCount; p++)
        {
            foreach (var (u, v) in bySeat[p])
            {
                if (solver.BooleanValue(v))
                {
                    occupant[p] = u;
                }
            }
        }

        return (status, occupant);
    }

    private static void AddAdjacencyRules(SeatingContext context, CpModel model, List<(int Unit, BoolVar Var)>[] bySeat, LinearExprBuilder objective)
    {
        foreach (var rule in context.AdjacencyRules)
        {
            var parameters = rule.Parameters;
            var values = ConflictingValues(context, parameters.AttributeKey);
            if (values.Count == 0)
            {
                continue;
            }

            var edges = context.Edges(parameters.Adjacency, parameters.SeatLevelSide);
            foreach (var value in values)
            {
                bool HasValue(int unit) => string.Equals(context.Units[unit].ValueOf(parameters.AttributeKey), value, StringComparison.Ordinal);

                foreach (var edge in edges)
                {
                    // A_{v,p} + A_{v,q} ≤ 1 (hard) or ≤ 1 + viol (soft).
                    var left = bySeat[edge.A].Where(x => HasValue(x.Unit)).Select(x => (ILiteral)x.Var).ToList();
                    var right = bySeat[edge.B].Where(x => HasValue(x.Unit)).Select(x => (ILiteral)x.Var).ToList();
                    if (left.Count == 0 || right.Count == 0)
                    {
                        continue;
                    }

                    var literals = left.Concat(right).ToList();
                    if (rule.Rule.IsHard)
                    {
                        model.AddAtMostOne(literals);
                    }
                    else
                    {
                        var violation = model.NewBoolVar(string.Empty);
                        model.Add(LinearExpr.Sum(literals) - violation <= MaxOccupantsPerSeat);
                        objective.AddTerm(violation, rule.Rule.Weight);
                    }
                }
            }
        }
    }

    private static void AddSpreadRules(SeatingContext context, CpModel model, List<(int Unit, BoolVar Var)>[] bySeat, LinearExprBuilder objective)
    {
        foreach (var rule in context.SpreadRules)
        {
            foreach (var edge in context.Edges(rule.Parameters.Adjacency, false))
            {
                if (bySeat[edge.A].Count == 0 || bySeat[edge.B].Count == 0)
                {
                    continue;
                }

                // pair_{p,q} ≥ occ_p + occ_q − 1.
                var pair = model.NewBoolVar(string.Empty);
                var occupied = bySeat[edge.A].Concat(bySeat[edge.B]).Select(x => (ILiteral)x.Var);
                model.Add(LinearExpr.Sum(occupied) - pair <= MaxOccupantsPerSeat);
                objective.AddTerm(pair, rule.Rule.Weight);
            }
        }
    }

    private static void AddFillRules(SeatingContext context, Dictionary<int, BoolVar>[] vars, LinearExprBuilder objective)
    {
        var weight = context.FillRules.Sum(r => (long)r.Weight);
        if (weight == 0)
        {
            return;
        }

        // w · Σ_p row(p) · occ_p.
        foreach (var unitVars in vars)
        {
            foreach (var (p, v) in unitVars)
            {
                objective.AddTerm(v, weight * context.Seats[p].Row);
            }
        }
    }

    /// <summary>Hamming distance ≥ 2 vs every previous seating: at least one previous (unit, seat) cell must change.</summary>
    private static void AddDiversity(SeatingContext context, CpModel model, Dictionary<int, BoolVar>[] vars)
    {
        var movableUnits = context.Units.Where(u => u.IsMovable).ToList();
        var movableCount = movableUnits.Sum(u => u.Count);
        var movableSeats = context.AllowedSeats
            .Where((_, u) => context.Units[u].IsMovable)
            .SelectMany(s => s)
            .Distinct()
            .Count();
        if (movableCount == 0 || (movableUnits.Count < MinCountForConflict && movableCount >= movableSeats))
        {
            return;
        }

        var seatIndexByKey = Enumerable.Range(0, context.Seats.Count).ToDictionary(p => context.Seats[p].Key, StringComparer.Ordinal);
        foreach (var previous in context.Request.PreviousSeatings)
        {
            var kept = new List<ILiteral>();
            var cells = 0;
            foreach (var assignment in previous)
            {
                var unit = context.FindUnit(assignment);
                if (unit < 0 || !context.Units[unit].IsMovable)
                {
                    continue;
                }

                cells++;
                if (seatIndexByKey.TryGetValue(assignment.SeatKey, out var p) && vars[unit].TryGetValue(p, out var v))
                {
                    kept.Add(v);
                }
            }

            if (cells > 0 && kept.Count >= cells)
            {
                model.Add(LinearExpr.Sum(kept) <= cells - MinHammingChanges);
            }
        }
    }

    private static SeatingResult Result(SeatingContext context, SolveStatus status, int[] occupant)
    {
        var assignments = new List<ClassSeatAssignment>();
        for (var p = 0; p < occupant.Length; p++)
        {
            if (occupant[p] >= 0)
            {
                var unit = context.Units[occupant[p]];
                assignments.Add(new ClassSeatAssignment(unit.ClassIndex, unit.PinnedParticipantId, context.Seats[p].Key));
            }
        }

        var penaltyByRule = SeatingPenalties.Evaluate(context, occupant);
        return new SeatingResult(status, assignments, penaltyByRule.Values.Sum(), penaltyByRule);
    }
}