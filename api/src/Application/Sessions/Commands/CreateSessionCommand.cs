using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Sessions.Exceptions;
using Domain.Sessions;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.Sessions.Commands;

public record CreateSessionCommand : IRequest<Either<SessionException, ExamSession>>
{
    public string? Name { get; init; }

    public DateOnly? ExamDate { get; init; }

    public string? Description { get; init; }
}

public class CreateSessionCommandHandler(
    IExamSessionRepository sessionRepository,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CreateSessionCommand, Either<SessionException, ExamSession>>
{
    public Task<Either<SessionException, ExamSession>> Handle(
        CreateSessionCommand request,
        CancellationToken cancellationToken)
    {
        var id = ExamSessionId.New();
        var name = request.Name!.Trim();

        return EnsureNameIsUnique(id, name, cancellationToken)
            .BindAsync(_ => CreateEntity(id, name, request, cancellationToken));
    }

    private async Task<Either<SessionException, Unit>> EnsureNameIsUnique(ExamSessionId id, string name, CancellationToken cancellationToken)
    {
        var existing = await sessionRepository.FindByName(name, Option<ExamSessionId>.None, cancellationToken);
        return existing.Match<Either<SessionException, Unit>>(
            _ => new SessionAlreadyExistsException(id, name),
            () => Unit.Default);
    }

    private async Task<Either<SessionException, ExamSession>> CreateEntity(
        ExamSessionId id,
        string name,
        CreateSessionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var entity = ExamSession.New(id, name, request.ExamDate, request.Description, dateTimeProvider.UtcNow);
            return await sessionRepository.AddAsync(entity, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return new SessionArgumentException(id, exception.Message);
        }
        catch (Exception exception)
        {
            return new SessionUnknownException(id, exception);
        }
    }
}