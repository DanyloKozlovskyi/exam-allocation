using Domain.Participants;
using Domain.Rooms;
using Domain.Sessions;

namespace Domain.Pins;

public class Pin : IEntity<PinId>
{
    private Pin(PinId id, ExamSessionId sessionId, ParticipantId participantId, RoomId roomId, SeatId? seatId)
    {
        Id = id;
        SessionId = sessionId;
        ParticipantId = participantId;
        RoomId = roomId;
        SeatId = seatId;
    }

    public PinId Id { get; private set; }

    public ExamSessionId SessionId { get; private set; }

    public ParticipantId ParticipantId { get; private set; }

    public RoomId RoomId { get; private set; }

    public SeatId? SeatId { get; private set; }

    public static Pin New(PinId id, ExamSessionId sessionId, ParticipantId participantId, RoomId roomId, SeatId? seatId)
        => new(id, sessionId, participantId, roomId, seatId);

    public void MoveTo(RoomId roomId, SeatId? seatId)
    {
        RoomId = roomId;
        SeatId = seatId;
    }
}