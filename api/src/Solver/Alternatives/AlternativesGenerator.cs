using Solver.Model;

namespace Solver.Alternatives;

public sealed class AlternativesGenerator : IAlternativesGenerator
{
    public IReadOnlyList<SolvedAllocation> Generate(AlternativesRequest request, IProgress<SolveProgress> progress, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}