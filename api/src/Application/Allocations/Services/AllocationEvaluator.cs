using Application.Allocations.Models;
using Application.Common.Interfaces.Queries;
using Application.Solving.Mapping;
using Application.Solving.Services;
using Domain.Allocations;
using LanguageExt;
using Solver.Model;
using Solver.Validation;

namespace Application.Allocations.Services;

/// <summary>Loads stored allocations and evaluates (modified) assignments with the independent <see cref="IAllocationValidator"/>.</summary>
public class AllocationEvaluator(
    IAllocationQueries allocationQueries,
    ISolveRunQueries runQueries,
    IRoomQueries roomQueries,
    IParticipantQueries participantQueries,
    ISolverInputBuilder inputBuilder,
    IAllocationValidator validator) : IAllocationEvaluator
{
    public async Task<Option<AllocationContext>> Load(AllocationId allocationId, CancellationToken cancellationToken)
    {
        var allocation = await allocationQueries.GetById(allocationId, cancellationToken);
        if (allocation.IsNone)
        {
            return Option<AllocationContext>.None;
        }

        var stored = allocation.Match(x => x, () => throw new InvalidOperationException());
        var run = await runQueries.GetById(stored.RunId, cancellationToken);
        if (run.IsNone)
        {
            return Option<AllocationContext>.None;
        }

        var solveRun = run.Match(x => x, () => throw new InvalidOperationException());
        var rooms = await roomQueries.GetAllBySession(solveRun.SessionId, cancellationToken);
        var participants = await participantQueries.GetAllBySession(solveRun.SessionId, cancellationToken);
        var input = await inputBuilder.Build(solveRun.SessionId, solveRun.Options, cancellationToken);
        return new AllocationContext(stored, solveRun, rooms, participants, input);
    }

    public ValidationReport Validate(AllocationContext context, IReadOnlyList<SeatAssignment> items)
        => validator.Validate(context.Input, items);

    public MoveValidationReport ToMoveReport(ValidationReport report) => new(
        report.Violations.All(x => !x.IsHard),
        report.Violations
            .Select(x => new MoveViolation(SolverModelMapper.ToDomainViolationKind(x.Kind), x.IsHard, x.RuleId, x.ParticipantId, x.RoomId, x.SeatKey))
            .ToList(),
        report.SoftPenalty);

    public AllocationObjective ToObjective(AllocationContext context, IReadOnlyList<SeatAssignment> items, ValidationReport report)
    {
        var usedRooms = items.Select(x => x.RoomId).ToHashSet();
        var cost = context.Input.Rooms.Where(x => usedRooms.Contains(x.RoomId)).Sum(x => x.Cost);
        return new AllocationObjective
        {
            RoomsUsed = usedRooms.Count,
            Cost = cost,
            SoftPenalty = report.SoftPenalty,
            PenaltyByRule = report.PenaltyByRule.ToDictionary(x => x.Key, x => x.Value),
        };
    }
}