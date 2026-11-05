using Domain.SolveRuns;

namespace Application.Solving.Services;

public interface ISolveRunExecutor
{
    Task Execute(SolveRunId runId, CancellationToken cancellationToken);
}