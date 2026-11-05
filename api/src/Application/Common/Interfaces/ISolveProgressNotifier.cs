using Domain.SolveRuns;
using Solver.Model;

namespace Application.Common.Interfaces;

public interface ISolveProgressNotifier
{
    Task NotifyProgress(SolveRunId runId, SolveProgress progress, CancellationToken cancellationToken);

    Task NotifyCompleted(SolveRun run, CancellationToken cancellationToken);
}