using Application.Centers.Exceptions;
using Domain.Centers;
using LanguageExt;
using MediatR;

namespace Application.Centers.Commands;

public record DeleteTestCenterCommand : IRequest<Either<TestCenterException, TestCenter>>
{
    public Guid SessionId { get; init; }

    public Guid Id { get; init; }
}

public class DeleteTestCenterCommandHandler
    : IRequestHandler<DeleteTestCenterCommand, Either<TestCenterException, TestCenter>>
{
    public Task<Either<TestCenterException, TestCenter>> Handle(
        DeleteTestCenterCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}