using Solver.Model;

namespace Solver.Allocation;

public interface IClassBuilder
{
    IReadOnlyList<ParticipantClass> Build(SolverInput input);
}