using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Sessions.Exceptions;
using Domain.Sessions;
using LanguageExt;
using MediatR;

namespace Application.Sessions.Commands;

public record UpdateSessionCommand : IRequest<Either<SessionException, ExamSession>>
{
    public Guid Id { get; init; }

    public string? Name { get; init; }

    public DateOnly? ExamDate { get; init; }

    public string? Description { get; init; }

    public uint Version { get; init; }
}

public class UpdateSessionCommandHandler(
    IExamSessionRepository sessionRepository,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<UpdateSessionCommand, Either<SessionException, ExamSession>>
{
    public Task<Either<SessionException, ExamSession>> Handle(
        UpdateSessionCommand request,
        CancellationToken cancellationToken)
    {
        var id = new ExamSessionId(request.Id);
        var name = request.Name!.Trim();

        return GetSession(id, request.Version, cancellationToken)
            .BindAsync(session => EnsureNameIsUnique(session, name, cancellationToken))
            .BindAsync(session => UpdateEntity(session, name, request, cancellationToken));
    }

    private async Task<Either<SessionException, ExamSession>> GetSession(ExamSessionId id, uint version, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetById(id, cancellationToken);
        return session.Match<Either<SessionException, ExamSession>>(
            s => s.Version == version ? s : new SessionConcurrencyException(id),
            () => new SessionNotFoundException(id));
    }

    private async Task<Either<SessionException, ExamSession>> EnsureNameIsUnique(ExamSession session, string name, CancellationToken cancellationToken)
    {
        var existing = await sessionRepository.FindByName(name, session.Id, cancellationToken);
        return existing.Match<Either<SessionException, ExamSession>>(
            _ => new SessionAlreadyExistsException(session.Id, name),
            () => session);
    }

    private async Task<Either<SessionException, ExamSession>> UpdateEntity(
        ExamSession session,
        string name,
        UpdateSessionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            session.UpdateDetails(name, request.ExamDate, request.Description, dateTimeProvider.UtcNow);
            return await sessionRepository.UpdateAsync(session, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return new SessionArgumentException(session.Id, exception.Message);
        }
        catch (Exception exception)
        {
            return new SessionUnknownException(session.Id, exception);
        }
    }
}