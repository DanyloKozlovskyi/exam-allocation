using Domain.Allocations;
using Domain.Participants;
using Domain.Rooms;
using Domain.SolveRuns;
using Solver.Model;

namespace Application.Allocations.Services;

/// <summary>A stored allocation together with everything needed to validate or display it.</summary>
public sealed record AllocationContext(
    Allocation Allocation,
    SolveRun Run,
    IReadOnlyList<Room> Rooms,
    IReadOnlyList<Participant> Participants,
    SolverInput Input)
{
    public IReadOnlyDictionary<SeatId, (Room Room, Seat Seat)> SeatsById { get; } = Rooms
        .SelectMany(room => room.Seats.Select(seat => (seat.Id, room, seat)))
        .ToDictionary(x => x.Id, x => (x.room, x.seat));

    public IReadOnlyDictionary<(Guid RoomId, string SeatKey), SeatId> SeatIdsByKey { get; } = Rooms
        .SelectMany(room => room.Seats.Select(seat => (Key: (room.Id.Value, seat.Key.Value), seat.Id)))
        .ToDictionary(x => x.Key, x => x.Id);

    public IReadOnlyList<SeatAssignment> ToAssignments() => Allocation.Items
        .Where(item => SeatsById.ContainsKey(item.SeatId))
        .Select(item => new SeatAssignment(item.ParticipantId.Value, item.RoomId.Value, SeatsById[item.SeatId].Seat.Key.Value))
        .ToList();
}