using Solver.Model;

namespace Solver.Alternatives;

public interface IAlternativesGenerator
{
    IReadOnlyList<SolvedAllocation> Generate(AlternativesRequest request, IProgress<SolveProgress> progress, CancellationToken cancellationToken);
}