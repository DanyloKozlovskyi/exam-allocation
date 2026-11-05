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

public class UpdateSessionCommandHandler
    : IRequestHandler<UpdateSessionCommand, Either<SessionException, ExamSession>>
{
    public Task<Either<SessionException, ExamSession>> Handle(
        UpdateSessionCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}