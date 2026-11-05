using Application.Common.Errors;
using Domain.SolveRuns;

namespace Application.SolveRuns.Exceptions;

public abstract class SolveRunException(SolveRunId id, ErrorCode code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public SolveRunId SolveRunId { get; } = id;

    public ErrorCode Code { get; } = code;
}

public class SolveRunSessionNotFoundException(SolveRunId id, Guid sessionId)
    : SolveRunException(id, ErrorCode.SessionNotFound, $"Session with id {sessionId} does not exist")
{
    public Guid SessionId { get; } = sessionId;
}

public class SolveRunNotFoundException(SolveRunId id)
    : SolveRunException(id, ErrorCode.SolveRunNotFound, $"Solve run with id {id} does not exist");

public class SolveRunAlreadyActiveException(SolveRunId id)
    : SolveRunException(id, ErrorCode.SolveRunAlreadyActive, $"Solve run {id} is already queued or running for this session");

public class SolveRunNotCancellableException(SolveRunId id)
    : SolveRunException(id, ErrorCode.SolveRunNotCancellable, $"Solve run {id} is not active");

public class SolveRunArgumentException(SolveRunId id, string details)
    : SolveRunException(id, ErrorCode.InvalidArgument, $"{details}")
{
    public string Details { get; } = details;
}

public class SolveRunUnknownException(SolveRunId id, Exception innerException)
    : SolveRunException(id, ErrorCode.Unknown, $"Unknown exception for solverun {id}", innerException);