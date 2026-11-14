using System.Transactions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Imports.Exceptions;
using Application.Imports.Models;
using Application.Imports.Services;
using Domain.Participants;
using Domain.Sessions;
using Domain.ValueObjects;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.Imports.Commands;

public record CommitImportCommand : IRequest<Either<ImportException, ImportResult>>
{
    public Guid ImportId { get; init; }

    public ImportOptions? Options { get; init; }

    public bool SkipInvalid { get; init; }
}

/// <summary>
/// Validates the cached file again and writes all valid rows in one ambient transaction
/// (<see cref="ImportMode.Replace"/> removes the session's participants first). Rows matching an existing participant
/// by <see cref="ImportOptions.DedupeBy"/> update it; others are inserted. Rows with errors are skipped only when
/// <see cref="CommitImportCommand.SkipInvalid"/> is set, otherwise nothing is written.
/// </summary>
public class CommitImportCommandHandler(
    IImportCache cache,
    IParticipantFileReader reader,
    IParticipantRowValidator validator,
    IParticipantQueries participantQueries,
    IParticipantRepository participantRepository)
    : IRequestHandler<CommitImportCommand, Either<ImportException, ImportResult>>
{
    public async Task<Either<ImportException, ImportResult>> Handle(
        CommitImportCommand request,
        CancellationToken cancellationToken)
    {
        var options = request.Options!;
        try
        {
            var validated = await ImportPipeline.LoadAndValidate(
                cache, reader, validator, participantQueries, request.ImportId, options, cancellationToken);

            return await validated
                .Bind(x => EnsureCommittable(request, x.Validation).Map(_ => x))
                .MatchAsync<Either<ImportException, ImportResult>>(
                    async x =>
                    {
                        var result = await Write(x.Import.SessionId, options, x.Validation, x.Existing, cancellationToken);
                        cache.Remove(request.ImportId);
                        return result;
                    },
                    e => e);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ImportUnknownException(request.ImportId, ex);
        }
    }

    private static Either<ImportException, Unit> EnsureCommittable(CommitImportCommand request, ImportValidation validation)
        => validation.ErrorRows > 0 && !request.SkipInvalid
            ? new ImportRowsInvalidException(request.ImportId, validation.ErrorRows)
            : Unit.Default;

    private async Task<ImportResult> Write(
        Guid sessionGuid,
        ImportOptions options,
        ImportValidation validation,
        IReadOnlyList<Participant> existing,
        CancellationToken cancellationToken)
    {
        var sessionId = new ExamSessionId(sessionGuid);
        var index = ImportDedupe.Index(existing, options.DedupeBy);
        var inserted = new List<Participant>();
        var updated = 0;

        using var scope = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);
        if (options.Mode == ImportMode.Replace)
        {
            await participantRepository.RemoveAllBySession(sessionId, cancellationToken);
        }

        foreach (var row in validation.ValidRows)
        {
            var name = PersonName.From(row.LastName!, row.FirstName!, row.MiddleName);
            var attributes = row.Attributes.ToDictionary(x => AttributeKey.From(x.Key), x => x.Value);
            var match = ImportDedupe.FindMatch(row, index, options.DedupeBy);
            if (match is null)
            {
                inserted.Add(Participant.New(
                    ParticipantId.New(), sessionId, row.ExternalId, name, row.Group, row.School, row.NeedsAccessibleSeat, attributes, row.Notes));
                continue;
            }

            match.UpdateDetails(row.ExternalId, name, row.Group, row.School, row.NeedsAccessibleSeat, attributes, row.Notes);
            await participantRepository.UpdateAsync(match, cancellationToken);
            updated++;
        }

        if (inserted.Count > 0)
        {
            await participantRepository.AddRangeAsync(inserted, saveChanges: true, cancellationToken);
        }

        scope.Complete();
        return new ImportResult(inserted.Count, updated, validation.ErrorRows);
    }
}