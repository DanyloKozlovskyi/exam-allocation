using Application.Common.Errors;
using Domain.Allocations;

namespace Application.Allocations.Exceptions;

public abstract class AllocationException(AllocationId id, ErrorCode code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public AllocationId AllocationId { get; } = id;

    public ErrorCode Code { get; } = code;
}

public class AllocationNotFoundException(AllocationId id)
    : AllocationException(id, ErrorCode.AllocationNotFound, $"Allocation with id {id} does not exist");

public class AllocationMoveViolatesHardRuleException(AllocationId id, int violations)
    : AllocationException(id, ErrorCode.MoveViolatesHardRule, $"Moves violate {violations} hard rule(s)")
{
    public int Violations { get; } = violations;
}

public class AllocationInvalidMoveException(AllocationId id, string details)
    : AllocationException(id, ErrorCode.InvalidMove, $"{details}")
{
    public string Details { get; } = details;
}

public class AllocationArgumentException(AllocationId id, string details)
    : AllocationException(id, ErrorCode.InvalidArgument, $"{details}")
{
    public string Details { get; } = details;
}

public class AllocationUnknownException(AllocationId id, Exception innerException)
    : AllocationException(id, ErrorCode.Unknown, $"Unknown exception for allocation {id}", innerException);