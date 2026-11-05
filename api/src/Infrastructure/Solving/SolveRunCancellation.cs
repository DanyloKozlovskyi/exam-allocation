using System.Collections.Concurrent;
using Application.Common.Interfaces;
using Domain.SolveRuns;

namespace Infrastructure.Solving;

public class SolveRunCancellation : ISolveRunCancellation
{
    private readonly ConcurrentDictionary<SolveRunId, CancellationTokenSource> sources = new();

    public CancellationToken Register(SolveRunId runId, CancellationToken stoppingToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        sources[runId] = source;
        return source.Token;
    }

    public bool Cancel(SolveRunId runId)
    {
        if (!sources.TryGetValue(runId, out var source))
        {
            return false;
        }

        source.Cancel();
        return true;
    }

    public void Complete(SolveRunId runId)
    {
        if (sources.TryRemove(runId, out var source))
        {
            source.Dispose();
        }
    }
}