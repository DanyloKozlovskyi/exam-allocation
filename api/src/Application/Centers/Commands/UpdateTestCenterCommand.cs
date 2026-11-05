using Application.Centers.Exceptions;
using Domain.Centers;
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

public class UpdateTestCenterCommandHandler
    : IRequestHandler<UpdateTestCenterCommand, Either<TestCenterException, TestCenter>>
{
    public Task<Either<TestCenterException, TestCenter>> Handle(
        UpdateTestCenterCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}