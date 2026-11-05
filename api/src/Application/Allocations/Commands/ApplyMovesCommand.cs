using Application.Allocations.Exceptions;
using Application.Allocations.Models;
using Domain.Allocations;
using LanguageExt;
using MediatR;

namespace Application.Allocations.Commands;

public record ApplyMovesCommand : IRequest<Either<AllocationException, Allocation>>
{
    public Guid AllocationId { get; init; }

    public IReadOnlyList<SeatMove> Moves { get; init; } = [];

    public bool Force { get; init; }
}

public class ApplyMovesCommandHandler
    : IRequestHandler<ApplyMovesCommand, Either<AllocationException, Allocation>>
{
    public Task<Either<AllocationException, Allocation>> Handle(
        ApplyMovesCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}