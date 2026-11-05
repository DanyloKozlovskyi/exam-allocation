using Application.Common.Errors;
using Domain.Sessions;

namespace Application.Sessions.Exceptions;

public abstract class SessionException(ExamSessionId id, ErrorCode code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public ExamSessionId SessionId { get; } = id;

    public ErrorCode Code { get; } = code;
}

public class SessionNotFoundException(ExamSessionId id)
    : SessionException(id, ErrorCode.SessionNotFound, $"Session with id {id} does not exist");

public class SessionAlreadyExistsException(ExamSessionId id, string name)
    : SessionException(id, ErrorCode.SessionAlreadyExists, $"Session with name {name} already exists")
{
    public string Name { get; } = name;
}

public class SessionConcurrencyException(ExamSessionId id)
    : SessionException(id, ErrorCode.ConcurrencyConflict, $"Session {id} was modified by someone else");

public class SessionArgumentException(ExamSessionId id, string details)
    : SessionException(id, ErrorCode.InvalidArgument, $"{details}")
{
    public string Details { get; } = details;
}

public class SessionUnknownException(ExamSessionId id, Exception innerException)
    : SessionException(id, ErrorCode.Unknown, $"Unknown exception for session {id}", innerException);