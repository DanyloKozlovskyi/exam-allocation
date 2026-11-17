using Application.Allocations.Exceptions;
using Application.Allocations.Models;
using Application.Allocations.Services;
using Domain.Allocations;
using LanguageExt;
using MediatR;

namespace Application.Allocations.Commands;

public record ValidateMovesCommand : IRequest<Either<AllocationException, MoveValidationReport>>
{
    public Guid AllocationId { get; init; }

    public IReadOnlyList<SeatMove> Moves { get; init; } = [];
}

public class ValidateMovesCommandHandler(IAllocationEvaluator evaluator)
    : IRequestHandler<ValidateMovesCommand, Either<AllocationException, MoveValidationReport>>
{
    public async Task<Either<AllocationException, MoveValidationReport>> Handle(
        ValidateMovesCommand request,
        CancellationToken cancellationToken)
    {
        var allocationId = new AllocationId(request.AllocationId);
        var context = await evaluator.Load(allocationId, cancellationToken);
        if (context.IsNone)
        {
            return new AllocationNotFoundException(allocationId);
        }

        var loaded = context.Match(x => x, () => throw new InvalidOperationException());
        return SeatMoves.Apply(loaded, request.Moves)
            .Map(items => evaluator.ToMoveReport(evaluator.Validate(loaded, items)));
    }
}