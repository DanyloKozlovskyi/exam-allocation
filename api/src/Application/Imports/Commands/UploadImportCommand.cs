using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Settings;
using Application.Imports.Exceptions;
using Application.Imports.Models;
using Application.Imports.Services;
using Domain.Sessions;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.Imports.Commands;

public record UploadImportCommand : IRequest<Either<ImportException, ImportPreview>>
{
    public Guid SessionId { get; init; }

    public string? FileName { get; init; }

    public byte[] Content { get; init; } = [];
}

public class UploadImportCommandHandler(
    IExamSessionQueries sessionQueries,
    IParticipantFileReader reader,
    IColumnMappingSuggester suggester,
    IImportCache cache,
    ApplicationSettings settings)
    : IRequestHandler<UploadImportCommand, Either<ImportException, ImportPreview>>
{
    public async Task<Either<ImportException, ImportPreview>> Handle(
        UploadImportCommand request,
        CancellationToken cancellationToken)
    {
        var importId = Guid.CreateVersion7();
        try
        {
            var session = await sessionQueries.GetById(new ExamSessionId(request.SessionId), cancellationToken);
            if (session.IsNone)
            {
                return new ImportSessionNotFoundException(importId, request.SessionId);
            }

            return EnsureSize(importId, request.Content)
                .Bind(_ => ImportPipeline.Read(reader, importId, request.FileName!, request.Content))
                .Map(workbook => CreatePreview(importId, workbook))
                .Do(_ => cache.Put(new CachedImport(importId, request.SessionId, request.FileName!, request.Content)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ImportUnknownException(importId, ex);
        }
    }

    private Either<ImportException, Unit> EnsureSize(Guid importId, byte[] content)
    {
        var maxBytes = settings.Import.MaxFileSizeMb * ImportLimits.BytesPerMegabyte;
        return content.LongLength > maxBytes
            ? new ImportFileTooLargeException(importId, maxBytes)
            : Unit.Default;
    }

    private ImportPreview CreatePreview(Guid importId, WorkbookPreview workbook)
    {
        var sheet = workbook.Sheets[0];
        var headerIndex = ImportLimits.DefaultHeaderRow - 1;
        var headers = sheet.Rows.Count > headerIndex
            ? sheet.Rows[headerIndex].Select(x => x?.Trim() ?? string.Empty).ToList()
            : [];
        var sampleRows = sheet.Rows
            .Skip(ImportLimits.DefaultHeaderRow)
            .Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .Take(ImportPreview.SampleRowCount)
            .ToList();

        return new ImportPreview(
            importId,
            workbook.Sheets.Select(x => x.Name).ToList(),
            headers,
            sampleRows,
            suggester.Suggest(headers));
    }
}