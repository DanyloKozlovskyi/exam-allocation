using Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Solving;

/// <summary>Dequeues solve runs and executes them one at a time. Run execution is implemented in Phase 2 (ISolveRunExecutor).</summary>
public class SolveWorker(ISolveQueue queue, IServiceScopeFactory scopeFactory, ILogger<SolveWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var runId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<Application.Solving.Services.ISolveRunExecutor>();
                await executor.Execute(runId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Solve run {RunId} failed", runId);
            }
        }
    }
}