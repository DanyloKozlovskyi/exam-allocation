using Application.Common.Interfaces;
using Domain.SolveRuns;
using Microsoft.Extensions.Logging;
using Solver.Model;

namespace Application.Solving.Services;

/// <summary>Forwards solver progress to <see cref="ISolveProgressNotifier"/> without blocking the solver threads.</summary>
public sealed class NotifyingProgress(
    SolveRunId runId,
    ISolveProgressNotifier notifier,
    ILogger logger,
    CancellationToken cancellationToken) : IProgress<SolveProgress>
{
    public void Report(SolveProgress value)
    {
        _ = SendAsync(value);
    }

    private async Task SendAsync(SolveProgress value)
    {
        try
        {
            await notifier.NotifyProgress(runId, value, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Failed to publish progress for solve run {RunId}", runId);
        }
        catch (OperationCanceledException)
        {
            // Run cancelled: progress is no longer relevant.
        }
    }
}