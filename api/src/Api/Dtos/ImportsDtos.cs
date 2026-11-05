using Application.Imports.Commands;
using Application.Imports.Models;

namespace Api.Dtos;

public record ColumnMappingDto(IReadOnlyDictionary<ParticipantField, string> Fields, IReadOnlyDictionary<string, string> Attributes)
{
    public static ColumnMappingDto FromModel(ColumnMapping mapping) => new(mapping.Fields, mapping.Attributes);

    public ColumnMapping ToModel() => new(Fields, Attributes);
}

public record ImportPreviewDto(
    Guid ImportId,
    IReadOnlyList<string> Sheets,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string?>> SampleRows,
    ColumnMappingDto SuggestedMapping)
{
    public static ImportPreviewDto FromModel(ImportPreview preview)
        => new(preview.ImportId, preview.Sheets, preview.Headers, preview.SampleRows, ColumnMappingDto.FromModel(preview.SuggestedMapping));
}

public record ImportMappingDto(
    string Sheet,
    int HeaderRow,
    ColumnMappingDto Mapping,
    ImportMode Mode,
    DedupeStrategy DedupeBy,
    bool SkipInvalid = false)
{
    public ImportOptions ToOptions() => new(Sheet, HeaderRow, Mapping.ToModel(), Mode, DedupeBy);

    public ValidateImportCommand ToValidateCommand(Guid importId) => new() { ImportId = importId, Options = ToOptions() };

    public CommitImportCommand ToCommitCommand(Guid importId) => new() { ImportId = importId, Options = ToOptions(), SkipInvalid = SkipInvalid };
}

public record ImportErrorDto(int Row, string? Column, ImportErrorCode Code);

public record ImportValidationDto(int ValidRows, int ErrorRows, IReadOnlyList<ImportErrorDto> Errors, IReadOnlyList<int> Duplicates)
{
    public static ImportValidationDto FromModel(ImportValidation validation) => new(
        validation.ValidRows.Count,
        validation.ErrorRows,
        validation.Errors.Select(x => new ImportErrorDto(x.Row, x.Column, x.Code)).ToList(),
        validation.DuplicateRows);
}

public record ImportResultDto(int Inserted, int Updated, int Skipped)
{
    public static ImportResultDto FromModel(ImportResult result) => new(result.Inserted, result.Updated, result.Skipped);
}