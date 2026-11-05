using Application.Common.Errors;
using Domain.Rooms;

namespace Application.Rooms.Exceptions;

public abstract class RoomException(RoomId id, ErrorCode code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public RoomId RoomId { get; } = id;

    public ErrorCode Code { get; } = code;
}

public class RoomCenterNotFoundException(RoomId id, Guid centerId)
    : RoomException(id, ErrorCode.TestCenterNotFound, $"Test center with id {centerId} does not exist")
{
    public Guid CenterId { get; } = centerId;
}

public class RoomNotFoundException(RoomId id)
    : RoomException(id, ErrorCode.RoomNotFound, $"Room with id {id} does not exist");

public class RoomAlreadyExistsException(RoomId id, string name)
    : RoomException(id, ErrorCode.RoomAlreadyExists, $"Room with name {name} already exists")
{
    public string Name { get; } = name;
}

public class RoomSeatReferencedByPinException(RoomId id, IReadOnlyList<Guid> pinIds)
    : RoomException(id, ErrorCode.SeatReferencedByPin, $"Layout change removes seats referenced by pins")
{
    public IReadOnlyList<Guid> PinIds { get; } = pinIds;
}

public class RoomConcurrencyException(RoomId id)
    : RoomException(id, ErrorCode.ConcurrencyConflict, $"Room {id} was modified by someone else");

public class RoomArgumentException(RoomId id, string details)
    : RoomException(id, ErrorCode.InvalidArgument, $"{details}")
{
    public string Details { get; } = details;
}

public class RoomUnknownException(RoomId id, Exception innerException)
    : RoomException(id, ErrorCode.Unknown, $"Unknown exception for room {id}", innerException);