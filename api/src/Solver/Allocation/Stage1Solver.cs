using Solver.Model;

namespace Solver.Allocation;

public sealed class Stage1Solver : IStage1Solver
{
    public Stage1Result Solve(Stage1Request request, IProgress<SolveProgress> progress, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}