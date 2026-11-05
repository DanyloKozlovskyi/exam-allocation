using Domain.SolveRuns;

namespace Application.Common.Interfaces;

/// <summary>Tracks cancellation tokens of running solve runs.</summary>
public interface ISolveRunCancellation
{
    CancellationToken Register(SolveRunId runId, CancellationToken stoppingToken);

    bool Cancel(SolveRunId runId);

    void Complete(SolveRunId runId);
}