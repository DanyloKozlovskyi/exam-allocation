using Application.Common.Errors;

namespace Application.Imports.Exceptions;

public abstract class ImportException(Guid id, ErrorCode code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public Guid ImportId { get; } = id;

    public ErrorCode Code { get; } = code;
}

public class ImportSessionNotFoundException(Guid id, Guid sessionId)
    : ImportException(id, ErrorCode.SessionNotFound, $"Session with id {sessionId} does not exist")
{
    public Guid SessionId { get; } = sessionId;
}

public class ImportNotFoundException(Guid id)
    : ImportException(id, ErrorCode.ImportNotFound, $"Import {id} does not exist or has expired");

public class ImportFileInvalidException(Guid id, string details)
    : ImportException(id, ErrorCode.ImportFileInvalid, $"{details}")
{
    public string Details { get; } = details;
}

public class ImportFileTooLargeException(Guid id, long maxBytes)
    : ImportException(id, ErrorCode.ImportFileTooLarge, $"File exceeds the maximum size of {maxBytes} bytes")
{
    public long MaxBytes { get; } = maxBytes;
}

public class ImportRowsInvalidException(Guid id, int errorRows)
    : ImportException(id, ErrorCode.ImportRowsInvalid, $"{errorRows} rows contain errors")
{
    public int ErrorRows { get; } = errorRows;
}

public class ImportArgumentException(Guid id, string details)
    : ImportException(id, ErrorCode.InvalidArgument, $"{details}")
{
    public string Details { get; } = details;
}

public class ImportUnknownException(Guid id, Exception innerException)
    : ImportException(id, ErrorCode.Unknown, $"Unknown exception for import {id}", innerException);