using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Solving.Mapping;
using Domain.SolveRuns;
using Microsoft.Extensions.Logging;
using Solver.Model;
using Solver.Pipeline;

namespace Application.Solving.Services;

/// <summary>
/// Executes one queued solve run: Queued → Running → (Succeeded | Infeasible | Failed | Cancelled),
/// persisting allocations on success and publishing progress / completion.
/// </summary>
public class SolveRunExecutor(
    ISolveRunRepository runRepository,
    IAllocationRepository allocationRepository,
    ISolverInputBuilder inputBuilder,
    ISessionFingerprint fingerprint,
    ISolverPipeline pipeline,
    ISolveRunCancellation cancellation,
    ISolveProgressNotifier notifier,
    IDateTimeProvider dateTimeProvider,
    ILogger<SolveRunExecutor> logger) : ISolveRunExecutor
{
    public const string NoSolutionInTimeMessage = "No solution found within the time limit";

    public async Task Execute(SolveRunId runId, CancellationToken cancellationToken)
    {
        var found = await runRepository.GetById(runId, cancellationToken);
        if (found.IsNone)
        {
            logger.LogWarning("Solve run {RunId} not found", runId);
            return;
        }

        var run = found.Match(x => x, () => throw new InvalidOperationException());
        if (run.Status != SolveRunStatus.Queued)
        {
            logger.LogInformation("Solve run {RunId} skipped: status {Status}", runId, run.Status);
            return;
        }

        var runToken = cancellation.Register(runId, cancellationToken);
        try
        {
            // The run may have waited behind other sessions' runs: hash the inputs as they are now, right before
            // building the solver input, so isStale compares against what was actually solved.
            run.MarkRunning(dateTimeProvider.UtcNow, await fingerprint.Compute(run.SessionId, cancellationToken));
            if (!await runRepository.TryUpdate(run, cancellationToken))
            {
                // Cancelled (or otherwise completed) between loading and starting it.
                logger.LogInformation("Solve run {RunId} skipped: changed before start", runId);
                return;
            }

            await RunPipeline(run, runToken, cancellationToken);
        }
        catch (OperationCanceledException) when (runToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            await Complete(run, SolveRunStatus.Cancelled, [], null, null, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Solve run {RunId} failed", runId);
            await Complete(run, SolveRunStatus.Failed, [], null, exception.Message, cancellationToken, SolveRunFailureReason.SolverError);
        }
        finally
        {
            cancellation.Complete(runId);
        }
    }

    private async Task RunPipeline(SolveRun run, CancellationToken runToken, CancellationToken stoppingToken)
    {
        var input = await inputBuilder.Build(run.SessionId, run.Options, runToken);
        var progress = new NotifyingProgress(run.Id, notifier, logger, runToken);
        var output = await pipeline.RunAsync(input, progress, runToken);

        var status = SolverModelMapper.ToRunStatus(output.Status);
        var diagnostics = output.Diagnostics.Select(SolverModelMapper.ToDomainDiagnostic).ToList();
        var stats = SolverModelMapper.ToDomainStats(output.Stats);
        var message = output.Status == SolveStatus.NoSolutionInTime ? NoSolutionInTimeMessage : null;
        SolveRunFailureReason? reason = output.Status switch
        {
            SolveStatus.NoSolutionInTime => SolveRunFailureReason.NoSolutionInTime,
            SolveStatus.Failed when output.Diagnostics.Any(d => d.Code == Solver.Model.DiagnosticCode.ModelTooLarge) => SolveRunFailureReason.ModelTooLarge,
            _ => null,
        };

        if (status == SolveRunStatus.Succeeded && output.Allocations.Count != 0)
        {
            var allocations = await inputBuilder.ToAllocations(run.Id, run.SessionId, output, stoppingToken);
            await allocationRepository.AddRangeAsync(allocations, saveChanges: true, stoppingToken);
        }

        await Complete(run, status, diagnostics, stats, message, stoppingToken, reason);
    }

    private async Task Complete(
        SolveRun run,
        SolveRunStatus status,
        IReadOnlyList<SolveDiagnostic> diagnostics,
        SolveRunStats? stats,
        string? message,
        CancellationToken cancellationToken,
        SolveRunFailureReason? failureReason = null)
    {
        var current = await runRepository.GetById(run.Id, cancellationToken);
        var tracked = current.Match(x => x, () => run);
        if (!tracked.IsActive)
        {
            return;
        }

        tracked.Complete(status, diagnostics, stats, message, dateTimeProvider.UtcNow, failureReason);
        if (!await runRepository.TryUpdate(tracked, cancellationToken))
        {
            logger.LogInformation("Solve run {RunId} was completed concurrently; keeping that result", run.Id);
            return;
        }

        await notifier.NotifyCompleted(tracked, cancellationToken);
    }
}