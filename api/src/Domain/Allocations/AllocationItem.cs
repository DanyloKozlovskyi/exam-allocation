using Domain.Participants;
using Domain.Rooms;

namespace Domain.Allocations;

public class AllocationItem : IEntity<AllocationItemId>
{
    private AllocationItem(AllocationItemId id, AllocationId allocationId, ParticipantId participantId, RoomId roomId, SeatId seatId)
    {
        Id = id;
        AllocationId = allocationId;
        ParticipantId = participantId;
        RoomId = roomId;
        SeatId = seatId;
    }

    public AllocationItemId Id { get; private set; }

    public AllocationId AllocationId { get; private set; }

    public ParticipantId ParticipantId { get; private set; }

    public RoomId RoomId { get; private set; }

    public SeatId SeatId { get; private set; }

    public static AllocationItem New(AllocationItemId id, AllocationId allocationId, ParticipantId participantId, RoomId roomId, SeatId seatId)
        => new(id, allocationId, participantId, roomId, seatId);
}