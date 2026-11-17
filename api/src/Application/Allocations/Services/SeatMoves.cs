using Application.Allocations.Exceptions;
using Application.Allocations.Models;
using LanguageExt;
using Solver.Model;

namespace Application.Allocations.Services;

/// <summary>Applies seat moves to the assignments of a stored allocation; a move onto an occupied seat swaps the two participants.</summary>
public static class SeatMoves
{
    public static Either<AllocationException, IReadOnlyList<SeatAssignment>> Apply(AllocationContext context, IReadOnlyList<SeatMove> moves)
    {
        var byParticipant = context.ToAssignments().ToDictionary(x => x.ParticipantId);
        foreach (var move in moves)
        {
            if (!byParticipant.TryGetValue(move.ParticipantId, out var current))
            {
                return new AllocationInvalidMoveException(context.Allocation.Id, $"Participant {move.ParticipantId} is not part of this allocation");
            }

            if (!context.SeatIdsByKey.ContainsKey((move.RoomId, move.SeatKey)))
            {
                return new AllocationInvalidMoveException(context.Allocation.Id, $"Seat {move.SeatKey} does not exist in room {move.RoomId}");
            }

            var occupant = byParticipant.Values.FirstOrDefault(x => x.RoomId == move.RoomId && x.SeatKey == move.SeatKey && x.ParticipantId != move.ParticipantId);
            if (occupant is not null)
            {
                byParticipant[occupant.ParticipantId] = occupant with { RoomId = current.RoomId, SeatKey = current.SeatKey };
            }

            byParticipant[move.ParticipantId] = current with { RoomId = move.RoomId, SeatKey = move.SeatKey };
        }

        return byParticipant.Values.OrderBy(x => x.ParticipantId).ToList();
    }
}