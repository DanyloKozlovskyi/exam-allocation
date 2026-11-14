using Application.Centers.Exceptions;
using Application.Common.Interfaces.Repositories;
using Domain.Centers;
using Domain.Sessions;
using LanguageExt;
using MediatR;

namespace Application.Centers.Commands;

public record DeleteTestCenterCommand : IRequest<Either<TestCenterException, TestCenter>>
{
    public Guid SessionId { get; init; }

    public Guid Id { get; init; }
}

public class DeleteTestCenterCommandHandler(ITestCenterRepository centerRepository)
    : IRequestHandler<DeleteTestCenterCommand, Either<TestCenterException, TestCenter>>
{
    public Task<Either<TestCenterException, TestCenter>> Handle(
        DeleteTestCenterCommand request,
        CancellationToken cancellationToken)
    {
        var id = new TestCenterId(request.Id);
        var sessionId = new ExamSessionId(request.SessionId);

        return GetCenter(sessionId, id, cancellationToken)
            .BindAsync(center => DeleteEntity(center, cancellationToken));
    }

    private async Task<Either<TestCenterException, TestCenter>> GetCenter(ExamSessionId sessionId, TestCenterId id, CancellationToken cancellationToken)
    {
        var center = await centerRepository.GetById(sessionId, id, cancellationToken);
        return center.Match<Either<TestCenterException, TestCenter>>(
            c => c,
            () => new TestCenterNotFoundException(id));
    }

    private async Task<Either<TestCenterException, TestCenter>> DeleteEntity(TestCenter center, CancellationToken cancellationToken)
    {
        try
        {
            return await centerRepository.RemoveAsync(center, cancellationToken);
        }
        catch (Exception exception)
        {
            return new TestCenterUnknownException(center.Id, exception);
        }
    }
}