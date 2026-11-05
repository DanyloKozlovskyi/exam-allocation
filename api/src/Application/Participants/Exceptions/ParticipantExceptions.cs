using Application.Common.Errors;
using Domain.Participants;

namespace Application.Participants.Exceptions;

public abstract class ParticipantException(ParticipantId id, ErrorCode code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public ParticipantId ParticipantId { get; } = id;

    public ErrorCode Code { get; } = code;
}

public class ParticipantSessionNotFoundException(ParticipantId id, Guid sessionId)
    : ParticipantException(id, ErrorCode.SessionNotFound, $"Session with id {sessionId} does not exist")
{
    public Guid SessionId { get; } = sessionId;
}

public class ParticipantNotFoundException(ParticipantId id)
    : ParticipantException(id, ErrorCode.ParticipantNotFound, $"Participant with id {id} does not exist");

public class ParticipantAlreadyExistsException(ParticipantId id, string externalId)
    : ParticipantException(id, ErrorCode.ParticipantAlreadyExists, $"Participant with external id {externalId} already exists")
{
    public string ExternalId { get; } = externalId;
}

public class ParticipantConcurrencyException(ParticipantId id)
    : ParticipantException(id, ErrorCode.ConcurrencyConflict, $"Participant {id} was modified by someone else");

public class ParticipantArgumentException(ParticipantId id, string details)
    : ParticipantException(id, ErrorCode.InvalidArgument, $"{details}")
{
    public string Details { get; } = details;
}

public class ParticipantUnknownException(ParticipantId id, Exception innerException)
    : ParticipantException(id, ErrorCode.Unknown, $"Unknown exception for participant {id}", innerException);