using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Sessions.Models;
using Domain.Sessions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ExamSessionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<ExamSession, ExamSessionId>(dbContext), IExamSessionRepository, IExamSessionQueries
{
    public async Task<Option<ExamSession>> GetById(ExamSessionId id, CancellationToken cancellationToken)
    {
        var entity = await Context.ExamSessions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity ?? Option<ExamSession>.None;
    }

    public async Task<Option<ExamSession>> FindByName(string name, Option<ExamSessionId> excludeId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim();
        var query = Context.ExamSessions.AsNoTracking().Where(x => x.Name == normalized);
        query = excludeId.Match(id => query.Where(x => x.Id != id), () => query);
        var entity = await query.FirstOrDefaultAsync(cancellationToken);

        return entity ?? Option<ExamSession>.None;
    }

    public async Task<IReadOnlyList<ExamSession>> GetAll(CancellationToken cancellationToken)
    {
        return await Context.ExamSessions
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    async Task<Option<ExamSession>> IExamSessionQueries.GetById(ExamSessionId id, CancellationToken cancellationToken)
    {
        var entity = await Context.ExamSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity ?? Option<ExamSession>.None;
    }

    public async Task<Option<SessionTotals>> GetTotals(ExamSessionId id, CancellationToken cancellationToken)
    {
        var exists = await Context.ExamSessions.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);
        if (!exists)
        {
            return Option<SessionTotals>.None;
        }

        var centers = await Context.TestCenters
            .AsNoTracking()
            .CountAsync(x => x.SessionId == id && x.IsActive, cancellationToken);

        var rooms = await Context.Rooms
            .AsNoTracking()
            .Where(x => x.IsActive && x.Center!.IsActive && x.Center.SessionId == id)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Capacity = g.Sum(x => x.Capacity),
                Accessible = g.Sum(x => x.AccessibleCapacity),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var participants = Context.Participants.AsNoTracking().Where(x => x.SessionId == id);
        var participantCount = await participants.CountAsync(cancellationToken);
        var needsAccessible = await participants.CountAsync(x => x.NeedsAccessibleSeat, cancellationToken);
        var groups = await participants
            .Where(x => x.Group != null)
            .Select(x => x.Group)
            .Distinct()
            .CountAsync(cancellationToken);

        var rules = await Context.Rules
            .AsNoTracking()
            .CountAsync(x => x.SessionId == id && x.IsEnabled, cancellationToken);

        return new SessionTotals(
            centers,
            rooms?.Count ?? 0,
            rooms?.Capacity ?? 0,
            rooms?.Accessible ?? 0,
            participantCount,
            needsAccessible,
            groups,
            rules);
    }
}