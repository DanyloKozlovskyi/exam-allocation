using Application.Allocations.Models;
using Domain.Allocations;
using LanguageExt;
using Solver.Model;

namespace Application.Allocations.Services;

public interface IAllocationEvaluator
{
    Task<Option<AllocationContext>> Load(AllocationId allocationId, CancellationToken cancellationToken);

    ValidationReport Validate(AllocationContext context, IReadOnlyList<SeatAssignment> items);

    MoveValidationReport ToMoveReport(ValidationReport report);

    AllocationObjective ToObjective(AllocationContext context, IReadOnlyList<SeatAssignment> items, ValidationReport report);
}