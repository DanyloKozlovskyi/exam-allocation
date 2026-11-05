namespace Application.Imports.Models;

public enum ParticipantField
{
    ExternalId = 1,
    LastName = 2,
    FirstName = 3,
    MiddleName = 4,
    FullName = 5,
    Group = 6,
    School = 7,
    NeedsAccessibleSeat = 8,
    Notes = 9,
}

public enum ImportMode
{
    Append = 1,
    Replace = 2,
}

public enum DedupeStrategy
{
    None = 1,
    ExternalId = 2,
    FullName = 3,
}

public enum ImportErrorCode
{
    RequiredMissing = 1,
    ValueTooLong = 2,
    BooleanUnparseable = 3,
    DuplicateInFile = 4,
    DuplicateExisting = 5,
    EmptyRow = 6,
    AttributeKeyInvalid = 7,
}

public sealed record WorkbookSheet(string Name, IReadOnlyList<IReadOnlyList<string?>> Rows);

public sealed record WorkbookPreview(IReadOnlyList<WorkbookSheet> Sheets);

public sealed record ColumnMapping(
    IReadOnlyDictionary<ParticipantField, string> Fields,
    IReadOnlyDictionary<string, string> Attributes);

public sealed record ImportOptions(
    string Sheet,
    int HeaderRow,
    ColumnMapping Mapping,
    ImportMode Mode,
    DedupeStrategy DedupeBy);

public sealed record ImportPreview(
    Guid ImportId,
    IReadOnlyList<string> Sheets,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string?>> SampleRows,
    ColumnMapping SuggestedMapping)
{
    public const int SampleRowCount = 10;
}

public sealed record ImportRowError(int Row, string? Column, ImportErrorCode Code);

public sealed record ParsedParticipantRow(
    int Row,
    string? ExternalId,
    string? LastName,
    string? FirstName,
    string? MiddleName,
    string? Group,
    string? School,
    bool NeedsAccessibleSeat,
    IReadOnlyDictionary<string, string> Attributes,
    string? Notes);

public sealed record ImportValidation(
    IReadOnlyList<ParsedParticipantRow> ValidRows,
    int ErrorRows,
    IReadOnlyList<ImportRowError> Errors,
    IReadOnlyList<int> DuplicateRows);

public sealed record ImportResult(int Inserted, int Updated, int Skipped);

public sealed record CachedImport(Guid ImportId, Guid SessionId, string FileName, byte[] Content);