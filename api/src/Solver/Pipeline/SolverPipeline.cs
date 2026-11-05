using Solver.Model;

namespace Solver.Pipeline;

public sealed class SolverPipeline : ISolverPipeline
{
    public Task<SolverOutput> RunAsync(SolverInput input, IProgress<SolveProgress> progress, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}