using Solver.Model;

namespace Solver.Precheck;

public sealed class Precheck : IPrecheck
{
    public IReadOnlyList<Diagnostic> Run(SolverInput input, CancellationToken cancellationToken) => throw new NotImplementedException();
}