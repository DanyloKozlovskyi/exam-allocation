using Solver.Model;

namespace Solver.Validation;

/// <summary>
/// Independent, brute-force allocation checker (docs/04 §10). No CP-SAT and no reuse of the solver's
/// layout services: seats and adjacency are re-derived from <see cref="RoomLayoutModel"/>.
/// Hard violations make the report invalid; soft rules contribute <c>weight × units</c> to the penalty
/// using the penalty units of docs/04 §2. A missing attribute value is a unique value.
/// </summary>
public sealed class AllocationValidator : IAllocationValidator
{
    private static readonly AdjacencyKind[] NoExtraKinds = [];

    public ValidationReport Validate(SolverInput input, IReadOnlyList<SeatAssignment> items)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(items);

        var context = new Context(input);
        var violations = new List<Violation>();

        var placements = CheckAssignments(context, items, violations);
        CheckCoverage(context, items, violations);
        CheckSeatUniqueness(placements, violations);
        CheckCapacity(context, items, violations);
        CheckAccessibility(context, placements, violations);
        CheckPins(context, items, violations);

        var penaltyByRule = new Dictionary<Guid, long>();
        foreach (var rule in input.Rules)
        {
            var units = EvaluateRule(rule, placements, violations);
            if (units is { } value && !IsHardRule(rule))
            {
                penaltyByRule[rule.RuleId] = checked(value * rule.Weight);
            }
        }

        var softPenalty = penaltyByRule.Values.Sum();
        return new ValidationReport(!violations.Exists(x => x.IsHard), violations, softPenalty, penaltyByRule);
    }

    /// <summary>Soft-only rule types (docs/04 §2) are always treated as soft; R4 is always hard.</summary>
    private static bool IsHardRule(SolverRule rule) => rule.Type switch
    {
        RuleType.AccessibleSeating => true,
        RuleType.MinRoomFill or RuleType.KeepTogetherInCenter or RuleType.SpreadInRoom or RuleType.FillFromFront => false,
        _ => rule.IsHard,
    };

    private static List<Placement> CheckAssignments(Context context, IReadOnlyList<SeatAssignment> items, List<Violation> violations)
    {
        var placements = new List<Placement>(items.Count);
        foreach (var item in items)
        {
            if (!context.Participants.TryGetValue(item.ParticipantId, out var participant))
            {
                violations.Add(new Violation(ViolationKind.UnknownParticipant, true, null, item.ParticipantId, item.RoomId, item.SeatKey));
                continue;
            }

            if (!context.Rooms.TryGetValue(item.RoomId, out var room) || !room.Seats.TryGetValue(item.SeatKey, out var seat))
            {
                // Unknown room, missing or disabled seat.
                violations.Add(new Violation(ViolationKind.SeatNotFound, true, null, item.ParticipantId, item.RoomId, item.SeatKey));
                continue;
            }

            placements.Add(new Placement(participant, room, seat));
        }

        return placements
            .OrderBy(x => context.RoomOrder[x.Room.Room.RoomId])
            .ThenBy(x => x.Seat.Row)
            .ThenBy(x => x.Seat.Column)
            .ThenBy(x => x.Seat.SeatIndex)
            .ThenBy(x => x.Participant.SortKey, StringComparer.Ordinal)
            .ThenBy(x => x.Participant.ParticipantId)
            .ToList();
    }

    private static void CheckCoverage(Context context, IReadOnlyList<SeatAssignment> items, List<Violation> violations)
    {
        var counts = items.GroupBy(x => x.ParticipantId).ToDictionary(x => x.Key, x => x.Count());
        foreach (var participant in context.Input.Participants)
        {
            var count = counts.GetValueOrDefault(participant.ParticipantId);
            if (count == 0)
            {
                violations.Add(new Violation(ViolationKind.Unseated, true, null, participant.ParticipantId, null, null));
            }
            else if (count > 1)
            {
                violations.Add(new Violation(ViolationKind.SeatedMoreThanOnce, true, null, participant.ParticipantId, null, null));
            }
        }
    }

    private static void CheckSeatUniqueness(List<Placement> placements, List<Violation> violations)
    {
        foreach (var group in placements.GroupBy(x => (x.Room.Room.RoomId, x.Seat.Key)))
        {
            foreach (var extra in group.Skip(1))
            {
                violations.Add(new Violation(ViolationKind.SeatOccupiedMoreThanOnce, true, null, extra.Participant.ParticipantId, group.Key.RoomId, group.Key.Key));
            }
        }
    }

    private static void CheckCapacity(Context context, IReadOnlyList<SeatAssignment> items, List<Violation> violations)
    {
        var countByRoom = items.GroupBy(x => x.RoomId).ToDictionary(x => x.Key, x => x.Count());
        foreach (var room in context.Input.Rooms)
        {
            if (countByRoom.GetValueOrDefault(room.RoomId) > context.Rooms[room.RoomId].Capacity)
            {
                violations.Add(new Violation(ViolationKind.CapacityExceeded, true, null, null, room.RoomId, null));
            }
        }
    }

    private static void CheckAccessibility(Context context, List<Placement> placements, List<Violation> violations)
    {
        var ruleId = context.Input.Rules.FirstOrDefault(x => x.Type == RuleType.AccessibleSeating)?.RuleId;
        foreach (var placement in placements.Where(x => x.Participant.NeedsAccessibleSeat && !x.Seat.IsAccessible))
        {
            violations.Add(Violation(ViolationKind.AccessibilityViolated, true, ruleId, placement));
        }
    }

    private static void CheckPins(Context context, IReadOnlyList<SeatAssignment> items, List<Violation> violations)
    {
        var itemsByParticipant = items.ToLookup(x => x.ParticipantId);
        foreach (var pin in context.Input.Pins)
        {
            foreach (var item in itemsByParticipant[pin.ParticipantId])
            {
                var wrongRoom = item.RoomId != pin.RoomId;
                var wrongSeat = pin.SeatKey is not null && !string.Equals(pin.SeatKey, item.SeatKey, StringComparison.Ordinal);
                if (wrongRoom || wrongSeat)
                {
                    violations.Add(new Violation(ViolationKind.PinViolated, true, null, pin.ParticipantId, item.RoomId, item.SeatKey));
                }
            }
        }
    }

    /// <summary>Returns the number of penalty units for the rule (null for rules without penalty).</summary>
    private static long? EvaluateRule(SolverRule rule, List<Placement> placements, List<Violation> violations)
    {
        var isHard = IsHardRule(rule);
        return rule.Parameters switch
        {
            SeparateByAttributeParameters p => EvaluateSeparation(rule, isHard, p, placements, violations),
            NoAdjacentByAttributeParameters p => EvaluateNoAdjacent(rule, isHard, p, placements, violations),
            MinRoomFillParameters p => EvaluateMinRoomFill(rule, p, placements, violations),
            KeepTogetherInCenterParameters p => EvaluateKeepTogether(rule, p, placements, violations),
            SpreadInRoomParameters p => EvaluateSpread(rule, p, placements, violations),
            FillFromFrontParameters => EvaluateFillFromFront(rule, placements, violations),
            AccessibleSeatingParameters => null,
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule.Parameters.GetType().Name, "Unsupported rule parameters"),
        };
    }

    /// <summary>R2: per scope unit and value, every participant beyond <c>MaxPerScope</c> is one unit.</summary>
    private static long EvaluateSeparation(SolverRule rule, bool isHard, SeparateByAttributeParameters parameters, List<Placement> placements, List<Violation> violations)
    {
        long units = 0;
        var groups = placements.GroupBy(x => (
            Unit: parameters.Scope == RuleScope.Center ? x.Room.Room.CenterId : x.Room.Room.RoomId,
            Value: ValueKey(x.Participant, parameters.AttributeKey)));

        foreach (var group in groups)
        {
            var members = group
                .OrderBy(x => x.Participant.SortKey, StringComparer.Ordinal)
                .ThenBy(x => x.Participant.ParticipantId)
                .ToList();
            var excess = members.Count - parameters.MaxPerScope;
            if (excess <= 0)
            {
                continue;
            }

            units += excess;
            foreach (var member in members.Skip(Math.Max(parameters.MaxPerScope, 0)))
            {
                violations.Add(Violation(ViolationKind.SeparationViolated, isHard, rule.RuleId, member));
            }
        }

        return units;
    }

    /// <summary>R3: every adjacent pair of seats occupied by the same (non-missing) value is one unit.</summary>
    private static long EvaluateNoAdjacent(SolverRule rule, bool isHard, NoAdjacentByAttributeParameters parameters, List<Placement> placements, List<Violation> violations)
    {
        long units = 0;
        foreach (var (first, second) in AdjacentPairs(placements, parameters.Adjacency, parameters.SeatLevelSide))
        {
            var firstValue = Value(first.Participant, parameters.AttributeKey);
            if (firstValue is null || !string.Equals(firstValue, Value(second.Participant, parameters.AttributeKey), StringComparison.Ordinal))
            {
                continue;
            }

            units++;
            violations.Add(Violation(ViolationKind.AdjacencyViolated, isHard, rule.RuleId, second));
        }

        return units;
    }

    /// <summary>R5: for each used room, the number of seats missing to reach <c>⌈ratio · capacity⌉</c>.</summary>
    private static long EvaluateMinRoomFill(SolverRule rule, MinRoomFillParameters parameters, List<Placement> placements, List<Violation> violations)
    {
        long units = 0;
        foreach (var room in placements.GroupBy(x => x.Room))
        {
            var required = (long)Math.Ceiling((parameters.MinFillRatio * room.Key.Capacity) - SolverDefaults.FillRatioTolerance);
            var shortfall = required - room.Count();
            if (shortfall <= 0)
            {
                continue;
            }

            units += shortfall;
            violations.Add(new Violation(ViolationKind.MinRoomFillShortfall, false, rule.RuleId, null, room.Key.Room.RoomId, null));
        }

        return units;
    }

    /// <summary>R6: for each (non-missing) value, every center used beyond the first is one unit.</summary>
    private static long EvaluateKeepTogether(SolverRule rule, KeepTogetherInCenterParameters parameters, List<Placement> placements, List<Violation> violations)
    {
        long units = 0;
        var byValue = placements
            .Select(x => (Placement: x, Value: Value(x.Participant, parameters.AttributeKey)))
            .Where(x => x.Value is not null)
            .GroupBy(x => x.Value!, StringComparer.Ordinal);

        foreach (var value in byValue)
        {
            var centers = value
                .GroupBy(x => x.Placement.Room.Room.CenterId)
                .OrderByDescending(x => x.Count())
                .ThenBy(x => x.Key)
                .ToList();

            foreach (var extraCenter in centers.Skip(1))
            {
                units++;
                violations.Add(Violation(ViolationKind.KeepTogetherSplit, false, rule.RuleId, extraCenter.First().Placement));
            }
        }

        return units;
    }

    /// <summary>R8: every adjacent pair of occupied seats is one unit.</summary>
    private static long EvaluateSpread(SolverRule rule, SpreadInRoomParameters parameters, List<Placement> placements, List<Violation> violations)
    {
        long units = 0;
        foreach (var (_, second) in AdjacentPairs(placements, parameters.Adjacency, false))
        {
            units++;
            violations.Add(Violation(ViolationKind.SpreadViolated, false, rule.RuleId, second));
        }

        return units;
    }

    /// <summary>R9: Σ row index of occupied seats; one violation per room with a positive sum.</summary>
    private static long EvaluateFillFromFront(SolverRule rule, List<Placement> placements, List<Violation> violations)
    {
        long units = 0;
        foreach (var room in placements.GroupBy(x => x.Room))
        {
            long rows = room.Sum(x => (long)x.Seat.Row);
            if (rows == 0)
            {
                continue;
            }

            units += rows;
            violations.Add(new Violation(ViolationKind.FillFromFront, false, rule.RuleId, null, room.Key.Room.RoomId, null));
        }

        return units;
    }

    /// <summary>Brute force over all pairs of placements in the same room.</summary>
    private static IEnumerable<(Placement First, Placement Second)> AdjacentPairs(List<Placement> placements, IReadOnlyList<AdjacencyKind>? kinds, bool seatLevelSide)
    {
        var kindSet = (IReadOnlyCollection<AdjacencyKind>?)kinds?.ToHashSet() ?? NoExtraKinds;
        foreach (var room in placements.GroupBy(x => x.Room))
        {
            var members = room.ToList();
            for (var i = 0; i < members.Count; i++)
            {
                for (var j = i + 1; j < members.Count; j++)
                {
                    if (room.Key.AreAdjacent(members[i].Seat, members[j].Seat, kindSet, seatLevelSide))
                    {
                        yield return (members[i], members[j]);
                    }
                }
            }
        }
    }

    private static string? Value(SolverParticipant participant, string attributeKey)
        => participant.Attributes.TryGetValue(attributeKey, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    /// <summary>Grouping key where a missing value becomes a value unique to the participant.</summary>
    private static (string? Value, Guid? Unique) ValueKey(SolverParticipant participant, string attributeKey)
    {
        var value = Value(participant, attributeKey);
        return value is null ? (null, participant.ParticipantId) : (value, null);
    }

    private static Violation Violation(ViolationKind kind, bool isHard, Guid? ruleId, Placement placement)
        => new(kind, isHard, ruleId, placement.Participant.ParticipantId, placement.Room.Room.RoomId, placement.Seat.Key);

    private sealed record Placement(SolverParticipant Participant, ValidatorRoom Room, ValidatorSeat Seat);

    private sealed class Context
    {
        public Context(SolverInput input)
        {
            Input = input;
            Rooms = input.Rooms.ToDictionary(x => x.RoomId, x => new ValidatorRoom(x));
            RoomOrder = input.Rooms.Select((x, i) => (x.RoomId, i)).ToDictionary(x => x.RoomId, x => x.i);
            Participants = input.Participants.ToDictionary(x => x.ParticipantId);
        }

        public SolverInput Input { get; }

        public IReadOnlyDictionary<Guid, ValidatorRoom> Rooms { get; }

        public IReadOnlyDictionary<Guid, int> RoomOrder { get; }

        public IReadOnlyDictionary<Guid, SolverParticipant> Participants { get; }
    }
}