using Application.Allocations.Exceptions;
using Application.Allocations.Models;
using LanguageExt;
using MediatR;

namespace Application.Allocations.Commands;

public record ValidateMovesCommand : IRequest<Either<AllocationException, MoveValidationReport>>
{
    public Guid AllocationId { get; init; }

    public IReadOnlyList<SeatMove> Moves { get; init; } = [];
}

public class ValidateMovesCommandHandler
    : IRequestHandler<ValidateMovesCommand, Either<AllocationException, MoveValidationReport>>
{
    public Task<Either<AllocationException, MoveValidationReport>> Handle(
        ValidateMovesCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}