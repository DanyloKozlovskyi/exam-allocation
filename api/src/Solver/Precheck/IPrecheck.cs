using Solver.Model;

namespace Solver.Precheck;

public interface IPrecheck
{
    IReadOnlyList<Diagnostic> Run(SolverInput input, CancellationToken cancellationToken);
}