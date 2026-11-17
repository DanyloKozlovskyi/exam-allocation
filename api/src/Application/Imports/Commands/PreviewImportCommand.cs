using Application.Common.Interfaces;
using Application.Imports.Exceptions;
using Application.Imports.Models;
using Application.Imports.Services;
using LanguageExt;
using MediatR;

namespace Application.Imports.Commands;

/// <summary>Re-reads a cached upload and returns the preview for the chosen sheet and header row.</summary>
public record PreviewImportCommand : IRequest<Either<ImportException, ImportPreview>>
{
    public Guid ImportId { get; init; }

    public string Sheet { get; init; } = string.Empty;

    public int HeaderRow { get; init; } = ImportLimits.DefaultHeaderRow;
}

public class PreviewImportCommandHandler(
    IImportCache cache,
    IParticipantFileReader reader,
    IColumnMappingSuggester suggester)
    : IRequestHandler<PreviewImportCommand, Either<ImportException, ImportPreview>>
{
    public Task<Either<ImportException, ImportPreview>> Handle(PreviewImportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = ImportPipeline.Load(cache, request.ImportId)
                .Bind(cached => ImportPipeline.Read(reader, request.ImportId, cached.FileName, cached.Content))
                .Bind(workbook => ImportPipeline.SelectSheet(workbook, request.ImportId, request.Sheet)
                    .Map(sheet => ImportPreviewBuilder.Build(request.ImportId, workbook, sheet, request.HeaderRow, suggester)));
            return Task.FromResult(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Task.FromResult<Either<ImportException, ImportPreview>>(new ImportUnknownException(request.ImportId, ex));
        }
    }
}