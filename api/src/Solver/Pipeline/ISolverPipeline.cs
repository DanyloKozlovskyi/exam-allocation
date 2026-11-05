using Solver.Model;

namespace Solver.Pipeline;

public interface ISolverPipeline
{
    Task<SolverOutput> RunAsync(SolverInput input, IProgress<SolveProgress> progress, CancellationToken cancellationToken);
}