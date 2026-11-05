using Application.Sessions.Exceptions;
using Domain.Sessions;
using LanguageExt;
using MediatR;

namespace Application.Sessions.Commands;

public record CreateSessionCommand : IRequest<Either<SessionException, ExamSession>>
{
    public string? Name { get; init; }

    public DateOnly? ExamDate { get; init; }

    public string? Description { get; init; }
}

public class CreateSessionCommandHandler
    : IRequestHandler<CreateSessionCommand, Either<SessionException, ExamSession>>
{
    public Task<Either<SessionException, ExamSession>> Handle(
        CreateSessionCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}