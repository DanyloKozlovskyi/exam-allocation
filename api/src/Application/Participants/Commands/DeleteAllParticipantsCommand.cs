using Application.Common.Interfaces.Repositories;
using Application.Participants.Exceptions;
using Domain.Participants;
using Domain.Sessions;
using LanguageExt;
using MediatR;

namespace Application.Participants.Commands;

public record DeleteAllParticipantsCommand : IRequest<Either<ParticipantException, int>>
{
    public Guid SessionId { get; init; }
}

public class DeleteAllParticipantsCommandHandler(
    IExamSessionRepository sessionRepository,
    IParticipantRepository participantRepository)
    : IRequestHandler<DeleteAllParticipantsCommand, Either<ParticipantException, int>>
{
    public Task<Either<ParticipantException, int>> Handle(
        DeleteAllParticipantsCommand request,
        CancellationToken cancellationToken)
    {
        var sessionId = new ExamSessionId(request.SessionId);

        return EnsureSessionExists(sessionId, cancellationToken)
            .BindAsync(_ => DeleteAll(sessionId, cancellationToken));
    }

    private async Task<Either<ParticipantException, ExamSession>> EnsureSessionExists(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetById(sessionId, cancellationToken);
        return session.Match<Either<ParticipantException, ExamSession>>(
            s => s,
            () => new ParticipantSessionNotFoundException(ParticipantId.Empty(), sessionId.Value));
    }

    private async Task<Either<ParticipantException, int>> DeleteAll(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        try
        {
            return await participantRepository.RemoveAllBySession(sessionId, cancellationToken);
        }
        catch (Exception exception)
        {
            return new ParticipantUnknownException(ParticipantId.Empty(), exception);
        }
    }
}