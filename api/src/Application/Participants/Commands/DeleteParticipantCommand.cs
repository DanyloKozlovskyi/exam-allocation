using Application.Common.Interfaces.Repositories;
using Application.Participants.Exceptions;
using Domain.Participants;
using Domain.Sessions;
using LanguageExt;
using MediatR;

namespace Application.Participants.Commands;

public record DeleteParticipantCommand : IRequest<Either<ParticipantException, Participant>>
{
    public Guid SessionId { get; init; }

    public Guid Id { get; init; }
}

public class DeleteParticipantCommandHandler(IParticipantRepository participantRepository)
    : IRequestHandler<DeleteParticipantCommand, Either<ParticipantException, Participant>>
{
    public Task<Either<ParticipantException, Participant>> Handle(
        DeleteParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var id = new ParticipantId(request.Id);
        var sessionId = new ExamSessionId(request.SessionId);

        return GetParticipant(sessionId, id, cancellationToken)
            .BindAsync(participant => DeleteEntity(participant, cancellationToken));
    }

    private async Task<Either<ParticipantException, Participant>> GetParticipant(
        ExamSessionId sessionId,
        ParticipantId id,
        CancellationToken cancellationToken)
    {
        var participant = await participantRepository.GetById(sessionId, id, cancellationToken);
        return participant.Match<Either<ParticipantException, Participant>>(
            p => p,
            () => new ParticipantNotFoundException(id));
    }

    private async Task<Either<ParticipantException, Participant>> DeleteEntity(Participant participant, CancellationToken cancellationToken)
    {
        try
        {
            return await participantRepository.RemoveAsync(participant, cancellationToken);
        }
        catch (Exception exception)
        {
            return new ParticipantUnknownException(participant.Id, exception);
        }
    }
}