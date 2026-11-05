using Domain.SolveRuns;

namespace Application.Common.Interfaces;

public interface ISolveQueue
{
    ValueTask EnqueueAsync(SolveRunId runId, CancellationToken cancellationToken);

    IAsyncEnumerable<SolveRunId> ReadAllAsync(CancellationToken cancellationToken);
}