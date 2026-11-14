using Application.Common.Interfaces.Repositories;
using Application.Participants.Exceptions;
using Application.Participants.Services;
using Domain.Participants;
using Domain.Sessions;
using Domain.ValueObjects;
using LanguageExt;
using MediatR;

namespace Application.Participants.Commands;

public record UpdateParticipantCommand : IRequest<Either<ParticipantException, Participant>>
{
    public Guid SessionId { get; init; }

    public Guid Id { get; init; }

    public string? ExternalId { get; init; }

    public string? LastName { get; init; }

    public string? FirstName { get; init; }

    public string? MiddleName { get; init; }

    public string? Group { get; init; }

    public string? School { get; init; }

    public bool NeedsAccessibleSeat { get; init; }

    public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>();

    public string? Notes { get; init; }

    public uint Version { get; init; }
}

public class UpdateParticipantCommandHandler(IParticipantRepository participantRepository)
    : IRequestHandler<UpdateParticipantCommand, Either<ParticipantException, Participant>>
{
    public Task<Either<ParticipantException, Participant>> Handle(
        UpdateParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var id = new ParticipantId(request.Id);
        var sessionId = new ExamSessionId(request.SessionId);

        return GetParticipant(sessionId, id, request.Version, cancellationToken)
            .BindAsync(participant => EnsureExternalIdIsUnique(participant, request.ExternalId, cancellationToken))
            .BindAsync(participant => UpdateEntity(participant, request, cancellationToken));
    }

    private async Task<Either<ParticipantException, Participant>> GetParticipant(
        ExamSessionId sessionId,
        ParticipantId id,
        uint version,
        CancellationToken cancellationToken)
    {
        var participant = await participantRepository.GetById(sessionId, id, cancellationToken);
        return participant.Match<Either<ParticipantException, Participant>>(
            p => p.Version == version ? p : new ParticipantConcurrencyException(id),
            () => new ParticipantNotFoundException(id));
    }

    private async Task<Either<ParticipantException, Participant>> EnsureExternalIdIsUnique(
        Participant participant,
        string? externalId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return participant;
        }

        var existing = await participantRepository.FindByExternalId(participant.SessionId, externalId, participant.Id, cancellationToken);
        return existing.Match<Either<ParticipantException, Participant>>(
            _ => new ParticipantAlreadyExistsException(participant.Id, externalId.Trim()),
            () => participant);
    }

    private async Task<Either<ParticipantException, Participant>> UpdateEntity(
        Participant participant,
        UpdateParticipantCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            participant.UpdateDetails(
                request.ExternalId,
                PersonName.From(request.LastName!, request.FirstName!, request.MiddleName),
                request.Group,
                request.School,
                request.NeedsAccessibleSeat,
                ParticipantAttributes.ToDomain(request.Attributes),
                request.Notes);
            return await participantRepository.UpdateAsync(participant, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return new ParticipantArgumentException(participant.Id, exception.Message);
        }
        catch (Exception exception)
        {
            return new ParticipantUnknownException(participant.Id, exception);
        }
    }
}