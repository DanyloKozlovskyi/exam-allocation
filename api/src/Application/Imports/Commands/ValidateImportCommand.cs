using Application.Imports.Exceptions;
using Application.Imports.Models;
using LanguageExt;
using MediatR;

namespace Application.Imports.Commands;

public record ValidateImportCommand : IRequest<Either<ImportException, ImportValidation>>
{
    public Guid ImportId { get; init; }

    public ImportOptions? Options { get; init; }
}

public class ValidateImportCommandHandler
    : IRequestHandler<ValidateImportCommand, Either<ImportException, ImportValidation>>
{
    public Task<Either<ImportException, ImportValidation>> Handle(
        ValidateImportCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}