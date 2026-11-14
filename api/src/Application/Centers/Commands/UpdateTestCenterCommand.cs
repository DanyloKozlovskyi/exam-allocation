using Application.Centers.Exceptions;
using Application.Common.Interfaces.Repositories;
using Domain.Centers;
using Domain.Sessions;
using Domain.ValueObjects;
using LanguageExt;
using MediatR;

namespace Application.Centers.Commands;

public record UpdateTestCenterCommand : IRequest<Either<TestCenterException, TestCenter>>
{
    public Guid SessionId { get; init; }

    public Guid Id { get; init; }

    public string? Name { get; init; }

    public string? Address { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string? GooglePlaceId { get; init; }

    public decimal? CostPerRoom { get; init; }

    public bool IsActive { get; init; }

    public uint Version { get; init; }
}

public class UpdateTestCenterCommandHandler(ITestCenterRepository centerRepository)
    : IRequestHandler<UpdateTestCenterCommand, Either<TestCenterException, TestCenter>>
{
    public Task<Either<TestCenterException, TestCenter>> Handle(
        UpdateTestCenterCommand request,
        CancellationToken cancellationToken)
    {
        var id = new TestCenterId(request.Id);
        var sessionId = new ExamSessionId(request.SessionId);
        var name = request.Name!.Trim();

        return GetCenter(sessionId, id, request.Version, cancellationToken)
            .BindAsync(center => EnsureNameIsUnique(center, name, cancellationToken))
            .BindAsync(center => UpdateEntity(center, name, request, cancellationToken));
    }

    private async Task<Either<TestCenterException, TestCenter>> GetCenter(
        ExamSessionId sessionId,
        TestCenterId id,
        uint version,
        CancellationToken cancellationToken)
    {
        var center = await centerRepository.GetById(sessionId, id, cancellationToken);
        return center.Match<Either<TestCenterException, TestCenter>>(
            c => c.Version == version ? c : new TestCenterConcurrencyException(id),
            () => new TestCenterNotFoundException(id));
    }

    private async Task<Either<TestCenterException, TestCenter>> EnsureNameIsUnique(
        TestCenter center,
        string name,
        CancellationToken cancellationToken)
    {
        var existing = await centerRepository.FindByName(center.SessionId, name, center.Id, cancellationToken);
        return existing.Match<Either<TestCenterException, TestCenter>>(
            _ => new TestCenterAlreadyExistsException(center.Id, name),
            () => center);
    }

    private async Task<Either<TestCenterException, TestCenter>> UpdateEntity(
        TestCenter center,
        string name,
        UpdateTestCenterCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            center.UpdateDetails(
                name,
                request.Address ?? string.Empty,
                GeoPoint.From(request.Latitude, request.Longitude),
                request.GooglePlaceId,
                request.CostPerRoom ?? TestCenterConstraints.DefaultCostPerRoom,
                request.IsActive);
            return await centerRepository.UpdateAsync(center, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return new TestCenterArgumentException(center.Id, exception.Message);
        }
        catch (Exception exception)
        {
            return new TestCenterUnknownException(center.Id, exception);
        }
    }
}