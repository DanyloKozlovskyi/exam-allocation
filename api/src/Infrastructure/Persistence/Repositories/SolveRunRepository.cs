using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Sessions;
using Domain.SolveRuns;
using Infrastructure.Persistence.Configurations;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Persistence.Repositories;

public class SolveRunRepository(ApplicationDbContext dbContext)
    : RepositoryBase<SolveRun, SolveRunId>(dbContext), ISolveRunRepository, ISolveRunQueries
{
    public async Task<Option<SolveRun>> GetById(SolveRunId id, CancellationToken cancellationToken)
    {
        var entity = await Context.SolveRuns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<SolveRun>.None;
    }

    public async Task<Option<SolveRun>> GetActive(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        var entity = await Context.SolveRuns
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId && (x.Status == SolveRunStatus.Queued || x.Status == SolveRunStatus.Running))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return entity ?? Option<SolveRun>.None;
    }

    public async Task<IReadOnlyList<SolveRun>> GetAllActive(CancellationToken cancellationToken)
    {
        return await Context.SolveRuns
            .AsNoTracking()
            .Where(x => x.Status == SolveRunStatus.Queued || x.Status == SolveRunStatus.Running)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SolveRun>> GetAll(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        return await Context.SolveRuns
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    async Task<Option<SolveRun>> ISolveRunQueries.GetById(SolveRunId id, CancellationToken cancellationToken)
    {
        var entity = await Context.SolveRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<SolveRun>.None;
    }

    public async Task<bool> TryAddActive(SolveRun entity, CancellationToken cancellationToken)
    {
        try
        {
            await Context.SolveRuns.AddAsync(entity, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: SolveRunsConfiguration.ActiveRunIndexName,
        })
        {
            return false;
        }
        finally
        {
            Context.ChangeTracker.Clear();
        }
    }

    public async Task<bool> TryUpdate(SolveRun entity, CancellationToken cancellationToken)
    {
        try
        {
            if (Context.Entry(entity).State == EntityState.Detached)
            {
                Context.SolveRuns.Update(entity);
            }

            await Context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        finally
        {
            Context.ChangeTracker.Clear();
        }
    }
}