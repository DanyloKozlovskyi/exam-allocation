using Application.Common.Errors;
using Domain.Pins;

namespace Application.Pins.Exceptions;

public abstract class PinException(PinId id, ErrorCode code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public PinId PinId { get; } = id;

    public ErrorCode Code { get; } = code;
}

public class PinSessionNotFoundException(PinId id, Guid sessionId)
    : PinException(id, ErrorCode.SessionNotFound, $"Session with id {sessionId} does not exist")
{
    public Guid SessionId { get; } = sessionId;
}

public class PinParticipantNotFoundException(PinId id, Guid participantId)
    : PinException(id, ErrorCode.ParticipantNotFound, $"Participant with id {participantId} does not exist")
{
    public Guid ParticipantId { get; } = participantId;
}

public class PinRoomNotFoundException(PinId id, Guid roomId)
    : PinException(id, ErrorCode.RoomNotFound, $"Room with id {roomId} does not exist")
{
    public Guid RoomId { get; } = roomId;
}

public class PinSeatNotFoundException(PinId id, string seatKey)
    : PinException(id, ErrorCode.SeatNotFound, $"Seat {seatKey} does not exist")
{
    public string SeatKey { get; } = seatKey;
}

public class PinConflictException(PinId id, string details)
    : PinException(id, ErrorCode.PinConflict, $"{details}")
{
    public string Details { get; } = details;
}

public class PinNotFoundException(PinId id)
    : PinException(id, ErrorCode.PinNotFound, $"Pin with id {id} does not exist");

public class PinArgumentException(PinId id, string details)
    : PinException(id, ErrorCode.InvalidArgument, $"{details}")
{
    public string Details { get; } = details;
}

public class PinUnknownException(PinId id, Exception innerException)
    : PinException(id, ErrorCode.Unknown, $"Unknown exception for pin {id}", innerException);