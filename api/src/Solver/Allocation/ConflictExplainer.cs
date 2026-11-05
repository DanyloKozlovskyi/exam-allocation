using Solver.Model;

namespace Solver.Allocation;

public sealed class ConflictExplainer : IConflictExplainer
{
    public IReadOnlyList<Diagnostic> Explain(Stage1Request request, CancellationToken cancellationToken) => throw new NotImplementedException();
}