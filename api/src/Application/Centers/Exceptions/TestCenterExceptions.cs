using Application.Common.Errors;
using Domain.Centers;

namespace Application.Centers.Exceptions;

public abstract class TestCenterException(TestCenterId id, ErrorCode code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public TestCenterId TestCenterId { get; } = id;

    public ErrorCode Code { get; } = code;
}

public class TestCenterSessionNotFoundException(TestCenterId id, Guid sessionId)
    : TestCenterException(id, ErrorCode.SessionNotFound, $"Session with id {sessionId} does not exist")
{
    public Guid SessionId { get; } = sessionId;
}

public class TestCenterNotFoundException(TestCenterId id)
    : TestCenterException(id, ErrorCode.TestCenterNotFound, $"Test center with id {id} does not exist");

public class TestCenterAlreadyExistsException(TestCenterId id, string name)
    : TestCenterException(id, ErrorCode.TestCenterAlreadyExists, $"Test center with name {name} already exists")
{
    public string Name { get; } = name;
}

public class TestCenterConcurrencyException(TestCenterId id)
    : TestCenterException(id, ErrorCode.ConcurrencyConflict, $"Test center {id} was modified by someone else");

public class TestCenterArgumentException(TestCenterId id, string details)
    : TestCenterException(id, ErrorCode.InvalidArgument, $"{details}")
{
    public string Details { get; } = details;
}

public class TestCenterUnknownException(TestCenterId id, Exception innerException)
    : TestCenterException(id, ErrorCode.Unknown, $"Unknown exception for testcenter {id}", innerException);