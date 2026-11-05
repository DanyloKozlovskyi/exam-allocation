using Application.Sessions.Exceptions;
using Domain.Sessions;
using LanguageExt;
using MediatR;

namespace Application.Sessions.Commands;

public record DeleteSessionCommand : IRequest<Either<SessionException, ExamSession>>
{
    public Guid Id { get; init; }
}

public class DeleteSessionCommandHandler
    : IRequestHandler<DeleteSessionCommand, Either<SessionException, ExamSession>>
{
    public Task<Either<SessionException, ExamSession>> Handle(
        DeleteSessionCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}