using Solver.Model;
using Solver.Validation;

namespace Solver.Tests.Pipeline.Fakes;

/// <summary>Brute-force oracle double for WS-A tests (seated once, seats, accessibility, pins, hard R2/R3).</summary>
public sealed class FakeValidator(bool alwaysInvalid = false) : IAllocationValidator
{
    private readonly FakeSeatGenerator seatGenerator = new();
    private readonly FakeAdjacencyBuilder adjacencyBuilder = new();

    public ValidationReport Validate(SolverInput input, IReadOnlyList<SeatAssignment> items)
    {
        var violations = new List<Violation>();
        if (alwaysInvalid)
        {
            violations.Add(new Violation(ViolationKind.Unseated, true, null, null, null, null));
        }

        var byParticipant = items.GroupBy(i => i.ParticipantId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var participant in input.Participants)
        {
            var count = byParticipant.GetValueOrDefault(participant.ParticipantId)?.Count ?? 0;
            if (count != 1)
            {
                violations.Add(new Violation(count == 0 ? ViolationKind.Unseated : ViolationKind.SeatedMoreThanOnce, true, null, participant.ParticipantId, null, null));
            }
        }

        var rooms = input.Rooms.ToDictionary(r => r.RoomId);
        var participants = input.Participants.ToDictionary(p => p.ParticipantId);
        foreach (var item in items)
        {
            var seat = rooms.TryGetValue(item.RoomId, out var room) ? seatGenerator.Generate(room.Layout).FirstOrDefault(s => s.Key == item.SeatKey) : null;
            if (seat is null)
            {
                violations.Add(new Violation(ViolationKind.SeatNotFound, true, null, item.ParticipantId, item.RoomId, item.SeatKey));
            }
            else if (participants[item.ParticipantId].NeedsAccessibleSeat && !seat.IsAccessible)
            {
                violations.Add(new Violation(ViolationKind.AccessibilityViolated, true, null, item.ParticipantId, item.RoomId, item.SeatKey));
            }
        }

        violations.AddRange(items.GroupBy(i => (i.RoomId, i.SeatKey)).Where(g => g.Count() > 1)
            .Select(g => new Violation(ViolationKind.SeatOccupiedMoreThanOnce, true, null, null, g.Key.RoomId, g.Key.SeatKey)));

        foreach (var pin in input.Pins)
        {
            var item = items.FirstOrDefault(i => i.ParticipantId == pin.ParticipantId);
            if (item is null || item.RoomId != pin.RoomId || (pin.SeatKey is not null && pin.SeatKey != item.SeatKey))
            {
                violations.Add(new Violation(ViolationKind.PinViolated, true, null, pin.ParticipantId, pin.RoomId, pin.SeatKey));
            }
        }

        foreach (var rule in input.Rules.Where(r => r.IsHard))
        {
            switch (rule.Parameters)
            {
                case SeparateByAttributeParameters p:
                    violations.AddRange(items
                        .Where(i => participants[i.ParticipantId].Attributes.ContainsKey(p.AttributeKey))
                        .GroupBy(i => (Unit: p.Scope == RuleScope.Room ? i.RoomId : rooms[i.RoomId].CenterId, Value: participants[i.ParticipantId].Attributes[p.AttributeKey]))
                        .Where(g => g.Count() > p.MaxPerScope)
                        .Select(g => new Violation(ViolationKind.SeparationViolated, true, rule.RuleId, null, null, null)));
                    break;
                case NoAdjacentByAttributeParameters p:
                    foreach (var roomItems in items.GroupBy(i => i.RoomId))
                    {
                        var layout = rooms[roomItems.Key].Layout;
                        var seats = seatGenerator.Generate(layout);
                        var valueBySeat = roomItems
                            .Where(i => participants[i.ParticipantId].Attributes.ContainsKey(p.AttributeKey))
                            .ToDictionary(i => i.SeatKey, i => participants[i.ParticipantId].Attributes[p.AttributeKey]);
                        foreach (var edge in adjacencyBuilder.Build(layout, seats, p.Adjacency, p.SeatLevelSide))
                        {
                            if (valueBySeat.TryGetValue(seats[edge.A].Key, out var a) && valueBySeat.TryGetValue(seats[edge.B].Key, out var b) && a == b)
                            {
                                violations.Add(new Violation(ViolationKind.AdjacencyViolated, true, rule.RuleId, null, roomItems.Key, seats[edge.A].Key));
                            }
                        }
                    }

                    break;
            }
        }

        return new ValidationReport(violations.Count == 0, violations, 0, new Dictionary<Guid, long>());
    }
}