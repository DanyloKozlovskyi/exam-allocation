using Application.Participants.Exceptions;
using LanguageExt;
using MediatR;

namespace Application.Participants.Commands;

public record DeleteAllParticipantsCommand : IRequest<Either<ParticipantException, int>>
{
    public Guid SessionId { get; init; }
}

public class DeleteAllParticipantsCommandHandler
    : IRequestHandler<DeleteAllParticipantsCommand, Either<ParticipantException, int>>
{
    public Task<Either<ParticipantException, int>> Handle(
        DeleteAllParticipantsCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}