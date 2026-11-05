using Application.Common.Errors;
using Domain.Rules;

namespace Application.Rules.Exceptions;

public abstract class RuleException(RuleId id, ErrorCode code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public RuleId RuleId { get; } = id;

    public ErrorCode Code { get; } = code;
}

public class RuleSessionNotFoundException(RuleId id, Guid sessionId)
    : RuleException(id, ErrorCode.SessionNotFound, $"Session with id {sessionId} does not exist")
{
    public Guid SessionId { get; } = sessionId;
}

public class RuleNotFoundException(RuleId id)
    : RuleException(id, ErrorCode.RuleNotFound, $"Rule with id {id} does not exist");

public class RuleParametersInvalidException(RuleId id, string details)
    : RuleException(id, ErrorCode.RuleParametersInvalid, $"{details}")
{
    public string Details { get; } = details;
}

public class RuleConcurrencyException(RuleId id)
    : RuleException(id, ErrorCode.ConcurrencyConflict, $"Rule {id} was modified by someone else");

public class RuleArgumentException(RuleId id, string details)
    : RuleException(id, ErrorCode.InvalidArgument, $"{details}")
{
    public string Details { get; } = details;
}

public class RuleUnknownException(RuleId id, Exception innerException)
    : RuleException(id, ErrorCode.Unknown, $"Unknown exception for rule {id}", innerException);