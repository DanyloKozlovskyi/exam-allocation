using Application.Imports.Exceptions;
using Application.Imports.Models;
using LanguageExt;
using MediatR;

namespace Application.Imports.Commands;

public record CommitImportCommand : IRequest<Either<ImportException, ImportResult>>
{
    public Guid ImportId { get; init; }

    public ImportOptions? Options { get; init; }

    public bool SkipInvalid { get; init; }
}

public class CommitImportCommandHandler
    : IRequestHandler<CommitImportCommand, Either<ImportException, ImportResult>>
{
    public Task<Either<ImportException, ImportResult>> Handle(
        CommitImportCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}