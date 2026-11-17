using Application.Common.Interfaces;

namespace Infrastructure.Persistence;

/// <summary>EF Core transaction over the scoped <see cref="ApplicationDbContext"/>; repositories in the same scope join it.</summary>
public class EfTransactionRunner(ApplicationDbContext context) : ITransactionRunner
{
    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is not null)
        {
            return await work(cancellationToken);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var result = await work(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}