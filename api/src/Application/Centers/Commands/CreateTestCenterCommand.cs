using Application.Centers.Exceptions;
using Domain.Centers;
using LanguageExt;
using MediatR;

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

public class CreateTestCenterCommandHandler
    : IRequestHandler<CreateTestCenterCommand, Either<TestCenterException, TestCenter>>
{
    public Task<Either<TestCenterException, TestCenter>> Handle(
        CreateTestCenterCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}