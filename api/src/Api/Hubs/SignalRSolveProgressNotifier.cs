using Application.Common.Interfaces;
using Domain.SolveRuns;
using Solver.Model;

namespace Api.Hubs;

public class SignalRSolveProgressNotifier : ISolveProgressNotifier
{
    public Task NotifyProgress(SolveRunId runId, SolveProgress progress, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task NotifyCompleted(SolveRun run, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}