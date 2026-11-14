using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Imports.Exceptions;
using Application.Imports.Models;
using Application.Imports.Services;
using LanguageExt;
using MediatR;

namespace Application.Imports.Commands;

public record ValidateImportCommand : IRequest<Either<ImportException, ImportValidation>>
{
    public Guid ImportId { get; init; }

    public ImportOptions? Options { get; init; }
}

public class ValidateImportCommandHandler(
    IImportCache cache,
    IParticipantFileReader reader,
    IParticipantRowValidator validator,
    IParticipantQueries participantQueries)
    : IRequestHandler<ValidateImportCommand, Either<ImportException, ImportValidation>>
{
    public async Task<Either<ImportException, ImportValidation>> Handle(
        ValidateImportCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ImportPipeline.LoadAndValidate(
                cache, reader, validator, participantQueries, request.ImportId, request.Options!, cancellationToken);
            return result.Map(x => x.Validation);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ImportUnknownException(request.ImportId, ex);
        }
    }
}