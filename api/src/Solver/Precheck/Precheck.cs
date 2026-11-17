using Solver.Allocation;
using Solver.Layout;
using Solver.Model;

namespace Solver.Precheck;

/// <summary>Fast necessary-condition checks before any CP-SAT model is built (04 §7.1).</summary>
public sealed class Precheck(ISeatGenerator seatGenerator, IAdjacencyBuilder adjacencyBuilder, IIndependentSetCalculator independentSetCalculator) : IPrecheck
{
    public IReadOnlyList<Diagnostic> Run(SolverInput input, CancellationToken cancellationToken)
    {
        var diagnostics = new List<Diagnostic>();
        var rooms = RoomFacts.Build(input.Rooms, seatGenerator);
        var totalCapacity = rooms.Sum(r => r.Capacity);
        if (totalCapacity == 0)
        {
            diagnostics.Add(DiagnosticFactory.Error(DiagnosticCode.NoActiveRooms));
        }

        CheckCapacity(input, rooms, totalCapacity, diagnostics);
        CheckAttributes(input, diagnostics);
        CheckPins(input, rooms, diagnostics);
        CheckSeparation(input, rooms, diagnostics);
        CheckAdjacency(input, rooms, diagnostics, cancellationToken);
        CheckPinRules(input, rooms, diagnostics);
        return diagnostics;
    }

    private void CheckCapacity(SolverInput input, IReadOnlyList<RoomFacts> rooms, int totalCapacity, List<Diagnostic> diagnostics)
    {
        if (totalCapacity > 0 && input.Participants.Count > totalCapacity)
        {
            diagnostics.Add(DiagnosticFactory.Error(
                DiagnosticCode.CapacityTotalExceeded,
                null,
                (DiagnosticArgument.Size, input.Participants.Count),
                (DiagnosticArgument.Limit, totalCapacity)));
        }

        var needsAccessible = input.Participants.Count(p => p.NeedsAccessibleSeat);
        var accessibleCapacity = rooms.Sum(r => r.AccessibleCapacity);
        if (needsAccessible > accessibleCapacity)
        {
            diagnostics.Add(DiagnosticFactory.Error(
                DiagnosticCode.AccessibleCapacityExceeded,
                RuleIdOf(input, RuleType.AccessibleSeating),
                (DiagnosticArgument.Size, needsAccessible),
                (DiagnosticArgument.Limit, accessibleCapacity)));
        }
    }

    private void CheckAttributes(SolverInput input, List<Diagnostic> diagnostics)
    {
        foreach (var key in AttributeValues.ReferencedKeys(input.Rules))
        {
            var missing = input.Participants.Count(p => !AttributeValues.TryGet(p, key, out _));
            if (missing > 0)
            {
                diagnostics.Add(DiagnosticFactory.Warning(
                    DiagnosticCode.AttributeMissing,
                    null,
                    (DiagnosticArgument.AttributeKey, key),
                    (DiagnosticArgument.Count, missing)));
            }
        }
    }

    private void CheckPins(SolverInput input, IReadOnlyList<RoomFacts> rooms, List<Diagnostic> diagnostics)
    {
        var roomsById = rooms.ToDictionary(r => r.Room.RoomId);
        foreach (var pin in input.Pins)
        {
            var roomMissing = !roomsById.TryGetValue(pin.RoomId, out var room);
            if (roomMissing || (pin.SeatKey is not null && !room!.SeatKeys.Contains(pin.SeatKey)))
            {
                diagnostics.Add(DiagnosticFactory.Error(
                    DiagnosticCode.PinInvalidSeat,
                    null,
                    (DiagnosticArgument.ParticipantId, pin.ParticipantId),
                    (DiagnosticArgument.RoomId, pin.RoomId),
                    (DiagnosticArgument.SeatKey, pin.SeatKey ?? string.Empty)));
            }
        }

        foreach (var duplicate in input.Pins
            .Where(p => p.SeatKey is not null)
            .GroupBy(p => (p.RoomId, p.SeatKey))
            .Where(g => g.Select(p => p.ParticipantId).Distinct().Count() > 1))
        {
            diagnostics.Add(DiagnosticFactory.Error(
                DiagnosticCode.PinDuplicateSeat,
                null,
                (DiagnosticArgument.RoomId, duplicate.Key.RoomId),
                (DiagnosticArgument.SeatKey, duplicate.Key.SeatKey!),
                (DiagnosticArgument.Count, duplicate.Count())));
        }

        foreach (var group in input.Pins.GroupBy(p => p.RoomId))
        {
            var count = group.Select(p => p.ParticipantId).Distinct().Count();
            if (roomsById.TryGetValue(group.Key, out var room) && count > room.Capacity)
            {
                diagnostics.Add(DiagnosticFactory.Error(
                    DiagnosticCode.PinRoomOverfull,
                    null,
                    (DiagnosticArgument.RoomId, group.Key),
                    (DiagnosticArgument.Size, count),
                    (DiagnosticArgument.Limit, room.Capacity)));
            }
        }
    }

    private void CheckSeparation(SolverInput input, IReadOnlyList<RoomFacts> rooms, List<Diagnostic> diagnostics)
    {
        foreach (var rule in input.Rules.Where(r => r.IsHard))
        {
            if (rule.Parameters is not SeparateByAttributeParameters parameters)
            {
                continue;
            }

            var units = parameters.Scope == RuleScope.Room
                ? rooms.Count(r => r.Capacity > 0)
                : rooms.Where(r => r.Capacity > 0).Select(r => r.Room.CenterId).Distinct().Count();
            var limit = (long)parameters.MaxPerScope * units;
            foreach (var (value, size) in ValueSizes(input.Participants, parameters.AttributeKey))
            {
                if (size > limit)
                {
                    diagnostics.Add(ValueDiagnostic(DiagnosticCode.SeparationImpossible, rule, parameters.AttributeKey, value, size, limit));
                }
            }
        }
    }

    private void CheckAdjacency(SolverInput input, IReadOnlyList<RoomFacts> rooms, List<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var alphaCache = new AlphaCache(independentSetCalculator);
        foreach (var rule in input.Rules.Where(r => r.IsHard))
        {
            if (rule.Parameters is not NoAdjacentByAttributeParameters parameters)
            {
                continue;
            }

            var sizes = ValueSizes(input.Participants, parameters.AttributeKey);
            if (sizes.Count == 0 || sizes.Max(s => s.Size) < 2)
            {
                continue;
            }

            long alphaTotal = rooms.Sum(r => (long)alphaCache.Get(r, parameters, cancellationToken));
            foreach (var (value, size) in sizes.Where(s => s.Size > alphaTotal))
            {
                diagnostics.Add(ValueDiagnostic(DiagnosticCode.NoAdjacencyImpossible, rule, parameters.AttributeKey, value, size, alphaTotal));
            }

            // Participants needing accessible seats can only use accessible seats (04 §7.1).
            var accessibleSizes = ValueSizes(input.Participants.Where(p => p.NeedsAccessibleSeat).ToList(), parameters.AttributeKey);
            if (accessibleSizes.Count == 0 || accessibleSizes.Max(s => s.Size) < 2)
            {
                continue;
            }

            long accessibleAlphaTotal = rooms.Sum(r => (long)alphaCache.GetAccessible(r, parameters, cancellationToken));
            foreach (var (value, size) in accessibleSizes.Where(s => s.Size > accessibleAlphaTotal && s.Size <= alphaTotal))
            {
                diagnostics.Add(ValueDiagnostic(DiagnosticCode.NoAdjacencyImpossible, rule, parameters.AttributeKey, value, size, accessibleAlphaTotal));
            }
        }
    }

    private void CheckPinRules(SolverInput input, IReadOnlyList<RoomFacts> rooms, List<Diagnostic> diagnostics)
    {
        var roomsById = rooms.ToDictionary(r => r.Room.RoomId);
        var keys = AttributeValues.ReferencedKeys(input.Rules);
        var pins = PinFacts.Build(input, keys);
        var pinned = pins.Pinned.Where(p => roomsById.ContainsKey(p.Pin.RoomId)).ToList();
        if (pinned.Count == 0)
        {
            return;
        }

        foreach (var rule in input.Rules.Where(r => r.IsHard))
        {
            switch (rule.Parameters)
            {
                case SeparateByAttributeParameters separate:
                    CheckPinnedSeparation(rule, separate, pinned, roomsById, diagnostics);
                    break;
                case NoAdjacentByAttributeParameters adjacency:
                    CheckPinnedAdjacency(rule, adjacency, pinned, roomsById, diagnostics);
                    break;
            }
        }

        CheckPinnedAccessibility(RuleIdOf(input, RuleType.AccessibleSeating), pinned, roomsById, diagnostics);
    }

    private void CheckPinnedSeparation(
        SolverRule rule,
        SeparateByAttributeParameters parameters,
        IReadOnlyList<(SolverPin Pin, SolverParticipant Participant)> pinned,
        IReadOnlyDictionary<Guid, RoomFacts> roomsById,
        List<Diagnostic> diagnostics)
    {
        var groups = pinned
            .Select(p => (Unit: parameters.Scope == RuleScope.Room ? p.Pin.RoomId : roomsById[p.Pin.RoomId].Room.CenterId, p.Participant))
            .Select(p => (p.Unit, HasValue: AttributeValues.TryGet(p.Participant, parameters.AttributeKey, out var value), Value: value))
            .Where(p => p.HasValue)
            .GroupBy(p => (p.Unit, p.Value!))
            .Where(g => g.Count() > parameters.MaxPerScope);
        foreach (var group in groups)
        {
            var unitArgument = parameters.Scope == RuleScope.Room ? DiagnosticArgument.RoomId : DiagnosticArgument.CenterId;
            diagnostics.Add(DiagnosticFactory.Error(
                DiagnosticCode.PinRuleConflict,
                rule.RuleId,
                (DiagnosticArgument.AttributeKey, parameters.AttributeKey),
                (DiagnosticArgument.Value, group.Key.Item2),
                (unitArgument, group.Key.Unit),
                (DiagnosticArgument.Size, group.Count()),
                (DiagnosticArgument.Limit, parameters.MaxPerScope)));
        }
    }

    private void CheckPinnedAdjacency(
        SolverRule rule,
        NoAdjacentByAttributeParameters parameters,
        IReadOnlyList<(SolverPin Pin, SolverParticipant Participant)> pinned,
        IReadOnlyDictionary<Guid, RoomFacts> roomsById,
        List<Diagnostic> diagnostics)
    {
        foreach (var roomGroup in pinned.Where(p => p.Pin.SeatKey is not null).GroupBy(p => p.Pin.RoomId))
        {
            var room = roomsById[roomGroup.Key];
            var seatIndex = room.Seats.Select((s, i) => (s.Key, i)).ToDictionary(e => e.Key, e => e.i, StringComparer.Ordinal);
            var valueBySeat = new Dictionary<int, string>();
            foreach (var (pin, participant) in roomGroup)
            {
                if (seatIndex.TryGetValue(pin.SeatKey!, out var index) && AttributeValues.TryGet(participant, parameters.AttributeKey, out var value))
                {
                    valueBySeat[index] = value;
                }
            }

            if (valueBySeat.Count < 2)
            {
                continue;
            }

            var edges = adjacencyBuilder.Build(room.Room.Layout, room.Seats, parameters.Adjacency, parameters.SeatLevelSide);
            foreach (var edge in edges)
            {
                if (valueBySeat.TryGetValue(edge.A, out var a) && valueBySeat.TryGetValue(edge.B, out var b) && a == b)
                {
                    diagnostics.Add(DiagnosticFactory.Error(
                        DiagnosticCode.PinRuleConflict,
                        rule.RuleId,
                        (DiagnosticArgument.AttributeKey, parameters.AttributeKey),
                        (DiagnosticArgument.Value, a),
                        (DiagnosticArgument.RoomId, room.Room.RoomId),
                        (DiagnosticArgument.SeatKey, room.Seats[edge.A].Key + DiagnosticFactory.ListSeparator + room.Seats[edge.B].Key)));
                }
            }
        }
    }

    private void CheckPinnedAccessibility(
        Guid? ruleId,
        IReadOnlyList<(SolverPin Pin, SolverParticipant Participant)> pinned,
        IReadOnlyDictionary<Guid, RoomFacts> roomsById,
        List<Diagnostic> diagnostics)
    {
        foreach (var (pin, participant) in pinned)
        {
            if (participant.NeedsAccessibleSeat
                && pin.SeatKey is not null
                && roomsById[pin.RoomId].SeatKeys.Contains(pin.SeatKey)
                && !roomsById[pin.RoomId].AccessibleKeys.Contains(pin.SeatKey))
            {
                diagnostics.Add(DiagnosticFactory.Error(
                    DiagnosticCode.PinRuleConflict,
                    ruleId,
                    (DiagnosticArgument.ParticipantId, participant.ParticipantId),
                    (DiagnosticArgument.RoomId, pin.RoomId),
                    (DiagnosticArgument.SeatKey, pin.SeatKey)));
            }
        }
    }

    private List<(string Value, int Size)> ValueSizes(IReadOnlyList<SolverParticipant> participants, string key)
        => participants
            .Select(p => (Has: AttributeValues.TryGet(p, key, out var value), Value: value))
            .Where(p => p.Has)
            .GroupBy(p => p.Value!, StringComparer.Ordinal)
            .Select(g => (g.Key, g.Count()))
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

    private Diagnostic ValueDiagnostic(DiagnosticCode code, SolverRule rule, string key, string value, int size, long limit)
        => DiagnosticFactory.Error(
            code,
            rule.RuleId,
            (DiagnosticArgument.AttributeKey, key),
            (DiagnosticArgument.Value, value),
            (DiagnosticArgument.Size, size),
            (DiagnosticArgument.Limit, limit));

    private Guid? RuleIdOf(SolverInput input, RuleType type)
        => input.Rules.FirstOrDefault(r => r.Type == type)?.RuleId;
}