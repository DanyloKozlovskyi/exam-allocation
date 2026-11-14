using Application.Common.Interfaces.Repositories;
using Application.Participants.Exceptions;
using Application.Participants.Services;
using Domain.Participants;
using Domain.Sessions;
using Domain.ValueObjects;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.Participants.Commands;

public record CreateParticipantCommand : IRequest<Either<ParticipantException, Participant>>
{
    public Guid SessionId { get; init; }

    public string? ExternalId { get; init; }

    public string? LastName { get; init; }

    public string? FirstName { get; init; }

    public string? MiddleName { get; init; }

    public string? Group { get; init; }

    public string? School { get; init; }

    public bool NeedsAccessibleSeat { get; init; }

    public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>();

    public string? Notes { get; init; }
}

public class CreateParticipantCommandHandler(
    IExamSessionRepository sessionRepository,
    IParticipantRepository participantRepository)
    : IRequestHandler<CreateParticipantCommand, Either<ParticipantException, Participant>>
{
    public Task<Either<ParticipantException, Participant>> Handle(
        CreateParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var id = ParticipantId.New();
        var sessionId = new ExamSessionId(request.SessionId);

        return EnsureSessionExists(id, sessionId, cancellationToken)
            .BindAsync(_ => EnsureExternalIdIsUnique(id, sessionId, request.ExternalId, cancellationToken))
            .BindAsync(_ => CreateEntity(id, sessionId, request, cancellationToken));
    }

    private async Task<Either<ParticipantException, ExamSession>> EnsureSessionExists(
        ParticipantId id,
        ExamSessionId sessionId,
        CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetById(sessionId, cancellationToken);
        return session.Match<Either<ParticipantException, ExamSession>>(
            s => s,
            () => new ParticipantSessionNotFoundException(id, sessionId.Value));
    }

    private async Task<Either<ParticipantException, Unit>> EnsureExternalIdIsUnique(
        ParticipantId id,
        ExamSessionId sessionId,
        string? externalId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return Unit.Default;
        }

        var existing = await participantRepository.FindByExternalId(sessionId, externalId, Option<ParticipantId>.None, cancellationToken);
        return existing.Match<Either<ParticipantException, Unit>>(
            _ => new ParticipantAlreadyExistsException(id, externalId.Trim()),
            () => Unit.Default);
    }

    private async Task<Either<ParticipantException, Participant>> CreateEntity(
        ParticipantId id,
        ExamSessionId sessionId,
        CreateParticipantCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var entity = Participant.New(
                id,
                sessionId,
                request.ExternalId,
                PersonName.From(request.LastName!, request.FirstName!, request.MiddleName),
                request.Group,
                request.School,
                request.NeedsAccessibleSeat,
                ParticipantAttributes.ToDomain(request.Attributes),
                request.Notes);
            return await participantRepository.AddAsync(entity, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return new ParticipantArgumentException(id, exception.Message);
        }
        catch (Exception exception)
        {
            return new ParticipantUnknownException(id, exception);
        }
    }
}