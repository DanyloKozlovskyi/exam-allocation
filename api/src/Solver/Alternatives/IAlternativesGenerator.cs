using Solver.Model;

namespace Solver.Alternatives;

public interface IAlternativesGenerator
{
    AlternativesResult Generate(AlternativesRequest request, IProgress<SolveProgress> progress, CancellationToken cancellationToken);
}