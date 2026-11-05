using Solver.Model;

namespace Solver.Allocation;

public interface IStage1Solver
{
    Stage1Result Solve(Stage1Request request, IProgress<SolveProgress> progress, CancellationToken cancellationToken);
}