using Google.OrTools.Sat;
using Solver.Model;

namespace Solver.Allocation;

/// <summary>Builds the stage-1 model of 04 §4; in explain mode every hard rule gets an enforcement literal (04 §7.2).</summary>
internal sealed class Stage1ModelBuilder
{
    private readonly Stage1Context context;
    private readonly bool explain;
    private readonly CancellationToken cancellationToken;
    private readonly CpModel model = new();
    private readonly Dictionary<(int ClassIndex, int RoomIndex), IntVar> counts = [];
    private readonly Dictionary<(int ClassIndex, int RoomIndex), long> upperBounds = [];
    private readonly Dictionary<Guid, LinearExprBuilder> penaltyByRule = [];
    private readonly List<(BoolVar Literal, ConflictItem Item)> enforcement = [];
    private readonly List<BoolVar> open = [];

    private Stage1ModelBuilder(Stage1Context context, bool explain, CancellationToken cancellationToken)
    {
        this.context = context;
        this.explain = explain;
        this.cancellationToken = cancellationToken;
    }

    /// <summary>Number of (class, room) pairs that would get a variable (04 §4 guardrail).</summary>
    public static long CountCells(Stage1Context context)
    {
        long cells = 0;
        foreach (var room in context.Rooms)
        {
            var free = room.Capacity - context.Pins.Count(room.Room.RoomId);
            if (free <= 0)
            {
                continue;
            }

            var freeAccessible = room.AccessibleCapacity - context.Pins.Accessible(room.Room.RoomId);
            cells += context.Classes.Count(c => !c.NeedsAccessibleSeat || freeAccessible > 0);
        }

        return cells;
    }

    public static Stage1Model Build(Stage1Context context, Stage1Request request, bool explain, CancellationToken cancellationToken)
    {
        var builder = new Stage1ModelBuilder(context, explain, cancellationToken);
        return builder.BuildModel(request);
    }

    private Stage1Model BuildModel(Stage1Request request)
    {
        var rules = context.Input.Rules;
        CreateVariables();
        AddCoverage();
        AddCapacity();
        AddAccessibility(rules.FirstOrDefault(r => r.Type == RuleType.AccessibleSeating));
        foreach (var rule in rules)
        {
            switch (rule.Parameters)
            {
                case SeparateByAttributeParameters separate:
                    AddSeparation(rule, separate);
                    break;
                case NoAdjacentByAttributeParameters adjacency when rule.IsHard:
                    AddAdjacencyBound(rule, adjacency);
                    break;
                case MinRoomFillParameters fill when !explain:
                    AddMinRoomFill(rule, fill);
                    break;
                case KeepTogetherInCenterParameters together when !explain:
                    AddKeepTogether(rule, together);
                    break;
            }
        }

        var costs = context.Rooms.Select(r => ScaledCost(r.Room.Cost)).ToArray();
        var cost = LinearExpr.WeightedSum(open, costs);
        if (!explain)
        {
            AddCuts(request.Cuts);
            AddSymmetryBreaking(request.Cuts, costs);
            if (request.CostBound is { } bound)
            {
                model.Add(cost <= bound);
            }

            foreach (var previous in request.PreviousSolutions)
            {
                AddDiversity(previous);
            }
        }

        var soft = LinearExpr.NewBuilder();
        foreach (var penalty in penaltyByRule.Values)
        {
            soft.Add(penalty);
        }

        var result = new Stage1Model
        {
            Model = model,
            Open = open,
            Counts = counts,
            Cost = cost,
            SoftPenalty = soft,
            PenaltyByRule = penaltyByRule.ToDictionary(e => e.Key, e => (LinearExpr)e.Value),
            HasSoftPenalty = penaltyByRule.Count > 0,
            Enforcement = enforcement,
        };
        if (!explain && request.Hint is { Count: > 0 } hint)
        {
            result.AddHint(hint, context.RoomIndexById);
        }

        return result;
    }

    public static long ScaledCost(decimal cost) => (long)decimal.Round(cost * SolverDefaults.CostScale, MidpointRounding.AwayFromZero);

    private void CreateVariables()
    {
        foreach (var room in context.Rooms)
        {
            var y = model.NewBoolVar($"y_{room.Index}");
            open.Add(y);
            var pinned = context.Pins.Count(room.Room.RoomId);
            if (pinned > 0)
            {
                model.Add(y == 1);
            }

            var free = room.Capacity - pinned;
            var freeAccessible = room.AccessibleCapacity - context.Pins.Accessible(room.Room.RoomId);
            foreach (var participantClass in context.Classes)
            {
                long upper = participantClass.Size;
                if (participantClass.NeedsAccessibleSeat && room.AccessibleCapacity == 0)
                {
                    continue;
                }

                if (!explain)
                {
                    upper = Math.Min(upper, free);
                    if (participantClass.NeedsAccessibleSeat)
                    {
                        upper = Math.Min(upper, freeAccessible);
                    }
                }

                if (upper <= 0)
                {
                    continue;
                }

                var cell = (participantClass.Index, room.Index);
                counts[cell] = model.NewIntVar(0, upper, $"n_{participantClass.Index}_{room.Index}");
                upperBounds[cell] = upper;
            }
        }
    }

    private void AddCoverage()
    {
        foreach (var participantClass in context.Classes)
        {
            var terms = context.Rooms
                .Select(r => counts.GetValueOrDefault((participantClass.Index, r.Index)))
                .OfType<IntVar>()
                .ToList();
            if (terms.Count == 0)
            {
                model.Add(model.NewConstant(0) == participantClass.Size);
                continue;
            }

            model.Add(LinearExpr.Sum(terms) == participantClass.Size);
        }
    }

    private void AddCapacity()
    {
        var literalByCenter = new Dictionary<Guid, BoolVar>();
        foreach (var room in context.Rooms)
        {
            var load = RoomLoad(room, context.Classes);
            var constraint = model.Add(load + context.Pins.Count(room.Room.RoomId) <= room.Capacity * open[room.Index]);
            if (explain)
            {
                if (!literalByCenter.TryGetValue(room.Room.CenterId, out var literal))
                {
                    literal = NewLiteral(new ConflictItem(null, room.Room.CenterId));
                    literalByCenter[room.Room.CenterId] = literal;
                }

                constraint.OnlyEnforceIf(literal);
            }
            else
            {
                // y_r = 0 forces an empty room; additionally an open room must not be empty unless pinned.
                model.Add(load + context.Pins.Count(room.Room.RoomId) >= open[room.Index]);
            }
        }
    }

    private void AddAccessibility(SolverRule? rule)
    {
        var literal = explain && rule is not null ? NewLiteral(new ConflictItem(rule.RuleId, null)) : null;
        var accessibleClasses = context.Classes.Where(c => c.NeedsAccessibleSeat).ToList();
        if (accessibleClasses.Count == 0)
        {
            return;
        }

        foreach (var room in context.Rooms)
        {
            var limit = room.AccessibleCapacity - context.Pins.Accessible(room.Room.RoomId);
            if (!explain && UpperBound(room, accessibleClasses) <= limit)
            {
                continue;
            }

            Enforce(model.Add(RoomLoad(room, accessibleClasses) <= limit), literal);
        }
    }

    private void AddSeparation(SolverRule rule, SeparateByAttributeParameters parameters)
    {
        var literal = explain && rule.IsHard ? NewLiteral(new ConflictItem(rule.RuleId, null)) : null;
        if (explain && !rule.IsHard)
        {
            return;
        }

        var byValue = context.ClassesByValue(parameters.AttributeKey);
        foreach (var unit in context.Units(parameters.Scope))
        {
            foreach (var value in context.Values(parameters.AttributeKey))
            {
                var classes = byValue.GetValueOrDefault(value) ?? [];
                var pinned = context.Pins.Value(parameters.AttributeKey, value, unit.Select(r => r.Room.RoomId));
                var upper = unit.Sum(r => UpperBound(r, classes)) + pinned;
                if (upper <= parameters.MaxPerScope && !explain)
                {
                    continue;
                }

                var load = UnitLoad(unit, classes);
                if (rule.IsHard)
                {
                    Enforce(model.Add(load + pinned <= parameters.MaxPerScope), literal);
                    continue;
                }

                var excess = model.NewIntVar(0, Math.Max(0, upper - parameters.MaxPerScope), $"excess_{rule.RuleId:N}");
                model.Add(excess >= load + pinned - parameters.MaxPerScope);
                Penalty(rule).AddTerm(excess, rule.Weight);
            }
        }
    }

    private void AddAdjacencyBound(SolverRule rule, NoAdjacentByAttributeParameters parameters)
    {
        var literal = explain ? NewLiteral(new ConflictItem(rule.RuleId, null)) : null;
        var byValue = context.ClassesByValue(parameters.AttributeKey);
        foreach (var room in context.Rooms)
        {
            foreach (var value in context.Values(parameters.AttributeKey))
            {
                var classes = byValue.GetValueOrDefault(value) ?? [];
                var pinned = context.Pins.Value(parameters.AttributeKey, value, room.Room.RoomId);
                var upper = UpperBound(room, classes) + pinned;
                if (upper < 2)
                {
                    continue;
                }

                var alpha = context.Alpha.Get(room, parameters, cancellationToken);
                if (upper <= alpha)
                {
                    continue;
                }

                Enforce(model.Add(RoomLoad(room, classes) + pinned <= alpha), literal);
            }
        }
    }

    private void AddMinRoomFill(SolverRule rule, MinRoomFillParameters parameters)
    {
        foreach (var room in context.Rooms.Where(r => r.Capacity > 0))
        {
            var target = (long)Math.Ceiling(parameters.MinFillRatio * room.Capacity);
            if (target <= 0)
            {
                continue;
            }

            var shortage = model.NewIntVar(0, target, $"short_{room.Index}");
            model.Add(shortage >= (target * open[room.Index]) - RoomLoad(room, context.Classes) - context.Pins.Count(room.Room.RoomId));
            Penalty(rule).AddTerm(shortage, rule.Weight);
        }
    }

    private void AddKeepTogether(SolverRule rule, KeepTogetherInCenterParameters parameters)
    {
        var byValue = context.ClassesByValue(parameters.AttributeKey);
        var pinTotals = context.Pins.ValueTotals(parameters.AttributeKey);
        var centers = context.Units(RuleScope.Center);
        foreach (var value in context.Values(parameters.AttributeKey))
        {
            var classes = byValue.GetValueOrDefault(value) ?? [];
            var size = classes.Sum(c => c.Size) + pinTotals.GetValueOrDefault(value);
            if (size < 2)
            {
                continue;
            }

            var penalty = Penalty(rule);
            penalty.Add(-rule.Weight);
            foreach (var center in centers)
            {
                var pinned = context.Pins.Value(parameters.AttributeKey, value, center.Select(r => r.Room.RoomId));
                var upper = center.Sum(r => UpperBound(r, classes));
                if (upper == 0)
                {
                    if (pinned > 0)
                    {
                        penalty.Add(rule.Weight);
                    }

                    continue;
                }

                var used = model.NewBoolVar($"z_{rule.RuleId:N}");
                model.Add(UnitLoad(center, classes) + pinned <= size * used);
                if (pinned > 0)
                {
                    model.Add(used == 1);
                }

                penalty.AddTerm(used, rule.Weight);
            }
        }
    }

    private void AddCuts(IReadOnlyList<Stage1Cut> cuts)
    {
        foreach (var cut in cuts)
        {
            if (!context.RoomIndexById.TryGetValue(cut.RoomId, out var roomIndex))
            {
                continue;
            }

            var room = context.Rooms[roomIndex];
            var classes = context.ClassesByValue(cut.AttributeKey).GetValueOrDefault(cut.Value) ?? [];
            var pinned = context.Pins.Value(cut.AttributeKey, cut.Value, cut.RoomId);
            model.Add(RoomLoad(room, classes) + pinned <= cut.MaxCount);
        }
    }

    private void AddSymmetryBreaking(IReadOnlyList<Stage1Cut> cuts, long[] costs)
    {
        var centerMatters = context.Input.Rules.Any(r => r.Parameters is KeepTogetherInCenterParameters
            || r.Parameters is SeparateByAttributeParameters { Scope: RuleScope.Center });
        var cutRooms = cuts.Select(c => c.RoomId).ToHashSet();
        var groups = context.Rooms
            .Where(r => context.Pins.Count(r.Room.RoomId) == 0 && !cutRooms.Contains(r.Room.RoomId))
            .GroupBy(r => (r.LayoutKey, Cost: costs[r.Index], Center: centerMatters ? r.Room.CenterId : Guid.Empty));
        foreach (var group in groups)
        {
            var ordered = group.OrderBy(r => r.Index).ToList();
            for (var i = 0; i + 1 < ordered.Count; i++)
            {
                model.Add(open[ordered[i].Index] >= open[ordered[i + 1].Index]);
            }
        }
    }

    private void AddDiversity(IReadOnlyList<RoomClassCount> previous)
    {
        var distance = (long)Math.Max(1, Math.Ceiling(context.Input.Options.DiversityRatio * context.MovableParticipants));
        var values = previous
            .Where(c => context.RoomIndexById.ContainsKey(c.RoomId))
            .GroupBy(c => (c.ClassIndex, context.RoomIndexById[c.RoomId]))
            .ToDictionary(g => g.Key, g => (long)g.Sum(c => c.Count));
        var total = LinearExpr.NewBuilder();
        foreach (var (cell, variable) in counts)
        {
            var before = values.GetValueOrDefault(cell);
            var difference = model.NewIntVar(0, Math.Max(before, upperBounds[cell]), $"diff_{cell.ClassIndex}_{cell.RoomIndex}");
            model.AddAbsEquality(difference, variable - before);
            total.Add(difference);
        }

        foreach (var (cell, before) in values.Where(v => !counts.ContainsKey(v.Key)))
        {
            total.Add(before);
        }

        model.Add(total >= 2 * distance);
    }

    private LinearExpr RoomLoad(RoomFacts room, IEnumerable<ParticipantClass> classes)
        => LinearExpr.Sum(classes.Select(c => counts.GetValueOrDefault((c.Index, room.Index))).OfType<IntVar>().Cast<LinearExpr>().Append(LinearExpr.Constant(0)));

    private LinearExpr UnitLoad(IReadOnlyList<RoomFacts> unit, IReadOnlyList<ParticipantClass> classes)
        => LinearExpr.Sum(unit.Select(r => RoomLoad(r, classes)));

    private long UpperBound(RoomFacts room, IEnumerable<ParticipantClass> classes)
        => classes.Sum(c => upperBounds.GetValueOrDefault((c.Index, room.Index)));

    private LinearExprBuilder Penalty(SolverRule rule)
    {
        if (!penaltyByRule.TryGetValue(rule.RuleId, out var builder))
        {
            builder = LinearExpr.NewBuilder();
            penaltyByRule[rule.RuleId] = builder;
        }

        return builder;
    }

    private BoolVar NewLiteral(ConflictItem item)
    {
        var literal = model.NewBoolVar($"b_{enforcement.Count}");
        enforcement.Add((literal, item));
        return literal;
    }

    private void Enforce(Constraint constraint, BoolVar? literal)
    {
        if (literal is not null)
        {
            constraint.OnlyEnforceIf(literal);
        }
    }
}