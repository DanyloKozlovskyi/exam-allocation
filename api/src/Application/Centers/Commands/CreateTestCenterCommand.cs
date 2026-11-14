using Application.Centers.Exceptions;
using Application.Common.Interfaces.Repositories;
using Domain.Centers;
using Domain.Sessions;
using Domain.ValueObjects;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.Centers.Commands;

public record CreateTestCenterCommand : IRequest<Either<TestCenterException, TestCenter>>
{
    public Guid SessionId { get; init; }

    public string? Name { get; init; }

    public string? Address { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string? GooglePlaceId { get; init; }

    public decimal? CostPerRoom { get; init; }

    public bool IsActive { get; init; }
}

public class CreateTestCenterCommandHandler(
    IExamSessionRepository sessionRepository,
    ITestCenterRepository centerRepository)
    : IRequestHandler<CreateTestCenterCommand, Either<TestCenterException, TestCenter>>
{
    public Task<Either<TestCenterException, TestCenter>> Handle(
        CreateTestCenterCommand request,
        CancellationToken cancellationToken)
    {
        var id = TestCenterId.New();
        var sessionId = new ExamSessionId(request.SessionId);
        var name = request.Name!.Trim();

        return EnsureSessionExists(id, sessionId, cancellationToken)
            .BindAsync(_ => EnsureNameIsUnique(id, sessionId, name, cancellationToken))
            .BindAsync(_ => CreateEntity(id, sessionId, name, request, cancellationToken));
    }

    private async Task<Either<TestCenterException, ExamSession>> EnsureSessionExists(
        TestCenterId id,
        ExamSessionId sessionId,
        CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetById(sessionId, cancellationToken);
        return session.Match<Either<TestCenterException, ExamSession>>(
            s => s,
            () => new TestCenterSessionNotFoundException(id, sessionId.Value));
    }

    private async Task<Either<TestCenterException, Unit>> EnsureNameIsUnique(
        TestCenterId id,
        ExamSessionId sessionId,
        string name,
        CancellationToken cancellationToken)
    {
        var existing = await centerRepository.FindByName(sessionId, name, Option<TestCenterId>.None, cancellationToken);
        return existing.Match<Either<TestCenterException, Unit>>(
            _ => new TestCenterAlreadyExistsException(id, name),
            () => Unit.Default);
    }

    private async Task<Either<TestCenterException, TestCenter>> CreateEntity(
        TestCenterId id,
        ExamSessionId sessionId,
        string name,
        CreateTestCenterCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var entity = TestCenter.New(
                id,
                sessionId,
                name,
                request.Address ?? string.Empty,
                GeoPoint.From(request.Latitude, request.Longitude),
                request.GooglePlaceId,
                request.CostPerRoom ?? TestCenterConstraints.DefaultCostPerRoom,
                request.IsActive);
            return await centerRepository.AddAsync(entity, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return new TestCenterArgumentException(id, exception.Message);
        }
        catch (Exception exception)
        {
            return new TestCenterUnknownException(id, exception);
        }
    }
}