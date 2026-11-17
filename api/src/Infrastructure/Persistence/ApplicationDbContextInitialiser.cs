using Application.Common.Interfaces;
using Domain.SolveRuns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public class ApplicationDbContextInitialiser(
    ApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    ILogger<ApplicationDbContextInitialiser> logger)
{
    public const string InterruptedRunMessage = "Interrupted by server restart";

    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (context.Database.IsRelational())
            {
                await context.Database.MigrateAsync(cancellationToken);
            }

            await FailInterruptedRunsAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while initialising the database");
            throw;
        }
    }

    public Task SeedDataAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private async Task FailInterruptedRunsAsync(CancellationToken cancellationToken)
    {
        var activeRuns = await context.SolveRuns
            .Where(x => x.Status == SolveRunStatus.Queued || x.Status == SolveRunStatus.Running)
            .ToListAsync(cancellationToken);

        foreach (var run in activeRuns)
        {
            run.Complete(SolveRunStatus.Failed, [], null, InterruptedRunMessage, dateTimeProvider.UtcNow, SolveRunFailureReason.Interrupted);
        }

        if (activeRuns.Count != 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}