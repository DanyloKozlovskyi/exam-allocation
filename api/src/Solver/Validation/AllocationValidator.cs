using Solver.Model;

namespace Solver.Validation;

public sealed class AllocationValidator : IAllocationValidator
{
    public ValidationReport Validate(SolverInput input, IReadOnlyList<SeatAssignment> items) => throw new NotImplementedException();
}