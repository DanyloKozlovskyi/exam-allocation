using Application.Participants.Exceptions;
using Domain.Participants;
using LanguageExt;
using MediatR;

namespace Application.Participants.Commands;

public record DeleteParticipantCommand : IRequest<Either<ParticipantException, Participant>>
{
    public Guid SessionId { get; init; }

    public Guid Id { get; init; }
}

public class DeleteParticipantCommandHandler
    : IRequestHandler<DeleteParticipantCommand, Either<ParticipantException, Participant>>
{
    public Task<Either<ParticipantException, Participant>> Handle(
        DeleteParticipantCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}