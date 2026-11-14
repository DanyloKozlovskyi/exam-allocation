using Application.Common.Interfaces.Repositories;
using Application.Sessions.Exceptions;
using Domain.Sessions;
using LanguageExt;
using MediatR;

namespace Application.Sessions.Commands;

public record DeleteSessionCommand : IRequest<Either<SessionException, ExamSession>>
{
    public Guid Id { get; init; }
}

public class DeleteSessionCommandHandler(IExamSessionRepository sessionRepository)
    : IRequestHandler<DeleteSessionCommand, Either<SessionException, ExamSession>>
{
    public Task<Either<SessionException, ExamSession>> Handle(
        DeleteSessionCommand request,
        CancellationToken cancellationToken)
    {
        var id = new ExamSessionId(request.Id);

        return GetSession(id, cancellationToken)
            .BindAsync(session => DeleteEntity(session, cancellationToken));
    }

    private async Task<Either<SessionException, ExamSession>> GetSession(ExamSessionId id, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetById(id, cancellationToken);
        return session.Match<Either<SessionException, ExamSession>>(
            s => s,
            () => new SessionNotFoundException(id));
    }

    private async Task<Either<SessionException, ExamSession>> DeleteEntity(ExamSession session, CancellationToken cancellationToken)
    {
        try
        {
            return await sessionRepository.RemoveAsync(session, cancellationToken);
        }
        catch (Exception exception)
        {
            return new SessionUnknownException(session.Id, exception);
        }
    }
}