using Domain.Participants;
using Domain.Pins;
using Domain.Rooms;
using Domain.Sessions;

namespace Tests.Data;

public static class PinsData
{
    public static Pin RoomPin(ExamSessionId sessionId, ParticipantId participantId, RoomId roomId)
        => Pin.New(PinId.New(), sessionId, participantId, roomId, null);

    public static Pin SeatPin(ExamSessionId sessionId, ParticipantId participantId, Room room, string seatKey)
        => Pin.New(PinId.New(), sessionId, participantId, room.Id, room.Seats.Single(x => x.Key.Value == seatKey).Id);
}