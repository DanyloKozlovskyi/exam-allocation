using Solver.Model;

namespace Solver.Allocation;

public interface IConflictExplainer
{
    IReadOnlyList<Diagnostic> Explain(Stage1Request request, CancellationToken cancellationToken);
}