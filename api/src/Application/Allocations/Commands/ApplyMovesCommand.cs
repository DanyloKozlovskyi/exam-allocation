using Application.Allocations.Exceptions;
using Application.Allocations.Models;
using Application.Allocations.Services;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Domain.Allocations;
using Domain.Participants;
using Domain.Rooms;
using LanguageExt;
using MediatR;
using Solver.Model;

namespace Application.Allocations.Commands;

public record ApplyMovesCommand : IRequest<Either<AllocationException, Allocation>>
{
    public Guid AllocationId { get; init; }

    public IReadOnlyList<SeatMove> Moves { get; init; } = [];

    public bool Force { get; init; }
}

/// <summary>Saves the moved assignments as a new manual allocation (index -1) of the same run.</summary>
public class ApplyMovesCommandHandler(
    IAllocationEvaluator evaluator,
    IAllocationRepository allocationRepository,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<ApplyMovesCommand, Either<AllocationException, Allocation>>
{
    public async Task<Either<AllocationException, Allocation>> Handle(
        ApplyMovesCommand request,
        CancellationToken cancellationToken)
    {
        var allocationId = new AllocationId(request.AllocationId);
        var context = await evaluator.Load(allocationId, cancellationToken);
        if (context.IsNone)
        {
            return new AllocationNotFoundException(allocationId);
        }

        var loaded = context.Match(x => x, () => throw new InvalidOperationException());
        var moved = SeatMoves.Apply(loaded, request.Moves);
        if (moved.IsLeft)
        {
            return moved.Match<Either<AllocationException, Allocation>>(_ => throw new InvalidOperationException(), e => e);
        }

        var items = moved.Match(x => x, _ => throw new InvalidOperationException());
        var report = evaluator.Validate(loaded, items);
        var hardViolations = report.Violations.Count(x => x.IsHard);
        if (hardViolations != 0 && !request.Force)
        {
            return new AllocationMoveViolatesHardRuleException(allocationId, hardViolations);
        }

        try
        {
            var manual = Allocation.New(
                AllocationId.New(),
                loaded.Allocation.RunId,
                AllocationConstraints.ManualIndex,
                evaluator.ToObjective(loaded, items, report),
                items.Select(x => ToItem(loaded, x)).ToList(),
                dateTimeProvider.UtcNow);
            return await allocationRepository.AddAsync(manual, cancellationToken);
        }
        catch (Exception exception)
        {
            return new AllocationUnknownException(allocationId, exception);
        }
    }

    private static (ParticipantId ParticipantId, RoomId RoomId, SeatId SeatId) ToItem(AllocationContext context, SeatAssignment item)
        => (new ParticipantId(item.ParticipantId), new RoomId(item.RoomId), context.SeatIdsByKey[(item.RoomId, item.SeatKey)]);
}