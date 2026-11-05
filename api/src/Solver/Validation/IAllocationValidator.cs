using Solver.Model;

namespace Solver.Validation;

public interface IAllocationValidator
{
    ValidationReport Validate(SolverInput input, IReadOnlyList<SeatAssignment> items);
}