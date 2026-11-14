using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Imports.Exceptions;
using Application.Imports.Models;
using Domain.Participants;
using Domain.Sessions;
using LanguageExt;

namespace Application.Imports.Services;

/// <summary>Steps shared by the import command handlers.</summary>
public static class ImportPipeline
{
    public static Either<ImportException, CachedImport> Load(IImportCache cache, Guid importId)
        => cache.Get(importId).ToEither<ImportException>(() => new ImportNotFoundException(importId));

    public static Either<ImportException, WorkbookPreview> Read(IParticipantFileReader reader, Guid importId, string fileName, byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content, writable: false);
            var workbook = reader.Read(stream, fileName);
            return workbook.Sheets.Count == 0
                ? new ImportFileInvalidException(importId, "The file contains no sheets")
                : workbook;
        }
        catch (InvalidDataException ex)
        {
            return new ImportFileInvalidException(importId, ex.Message);
        }
    }

    public static Either<ImportException, WorkbookSheet> SelectSheet(WorkbookPreview workbook, Guid importId, string sheetName)
        => workbook.Sheets.FirstOrDefault(x => string.Equals(x.Name, sheetName, StringComparison.OrdinalIgnoreCase)) is { } sheet
            ? sheet
            : new ImportArgumentException(importId, $"Sheet '{sheetName}' does not exist");

    /// <summary>Participants that imported rows are compared to: none in <see cref="ImportMode.Replace"/> mode.</summary>
    public static async Task<IReadOnlyList<Participant>> Existing(
        IParticipantQueries participantQueries,
        Guid sessionId,
        ImportMode mode,
        CancellationToken cancellationToken)
        => mode == ImportMode.Replace
            ? []
            : await participantQueries.GetAllBySession(new ExamSessionId(sessionId), cancellationToken);

    public static Either<ImportException, ImportValidation> Validate(
        IParticipantRowValidator validator,
        Guid importId,
        WorkbookSheet sheet,
        ImportOptions options,
        IReadOnlyList<Participant> existing)
    {
        try
        {
            return validator.Validate(sheet, options, existing);
        }
        catch (ArgumentException ex)
        {
            return new ImportArgumentException(importId, ex.Message);
        }
    }

    /// <summary>Loads the cached file and validates it with the given options.</summary>
    public static async Task<Either<ImportException, (CachedImport Import, ImportValidation Validation, IReadOnlyList<Participant> Existing)>> LoadAndValidate(
        IImportCache cache,
        IParticipantFileReader reader,
        IParticipantRowValidator validator,
        IParticipantQueries participantQueries,
        Guid importId,
        ImportOptions options,
        CancellationToken cancellationToken)
    {
        var loaded = Load(cache, importId)
            .Bind(import => Read(reader, importId, import.FileName, import.Content)
                .Bind(workbook => SelectSheet(workbook, importId, options.Sheet))
                .Map(sheet => (Import: import, Sheet: sheet)));

        return await loaded.MatchAsync<Either<ImportException, (CachedImport, ImportValidation, IReadOnlyList<Participant>)>>(
            async x =>
            {
                var existing = await Existing(participantQueries, x.Import.SessionId, options.Mode, cancellationToken);
                return Validate(validator, importId, x.Sheet, options, existing).Map(v => (x.Import, v, existing));
            },
            e => e);
    }
}