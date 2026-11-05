using Application.Imports.Exceptions;
using Application.Imports.Models;
using LanguageExt;
using MediatR;

namespace Application.Imports.Commands;

public record UploadImportCommand : IRequest<Either<ImportException, ImportPreview>>
{
    public Guid SessionId { get; init; }

    public string? FileName { get; init; }

    public byte[] Content { get; init; } = [];
}

public class UploadImportCommandHandler
    : IRequestHandler<UploadImportCommand, Either<ImportException, ImportPreview>>
{
    public Task<Either<ImportException, ImportPreview>> Handle(
        UploadImportCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}