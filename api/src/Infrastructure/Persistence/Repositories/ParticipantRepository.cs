using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Models;
using Application.Participants.Models;
using Domain.Participants;
using Domain.Sessions;
using Domain.ValueObjects;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ParticipantRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Participant, ParticipantId>(dbContext), IParticipantRepository, IParticipantQueries
{
    private const string LikeWildcard = "%";
    private const string LikeEscape = "\\";
    private const char LikeAny = '%';
    private const char LikeSingle = '_';

    public async Task<Option<Participant>> GetById(ExamSessionId sessionId, ParticipantId id, CancellationToken cancellationToken)
    {
        var entity = await Context.Participants
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.Id == id, cancellationToken);

        return entity ?? Option<Participant>.None;
    }

    public async Task<Option<Participant>> FindByExternalId(ExamSessionId sessionId, string externalId, Option<ParticipantId> excludeId, CancellationToken cancellationToken)
    {
        var normalized = externalId.Trim();
        var query = Context.Participants.AsNoTracking().Where(x => x.SessionId == sessionId && x.ExternalId == normalized);
        query = excludeId.Match(id => query.Where(x => x.Id != id), () => query);
        var entity = await query.FirstOrDefaultAsync(cancellationToken);

        return entity ?? Option<Participant>.None;
    }

    public async Task<int> RemoveAllBySession(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var removed = await Context.Participants
                .Where(x => x.SessionId == sessionId)
                .ExecuteDeleteAsync(cancellationToken);
            Context.ChangeTracker.Clear();
            return removed;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unexpected failure while trying to delete participants of session '{sessionId}'", ex);
        }
    }

    public async Task<PagedResult<Participant>> GetPage(ExamSessionId sessionId, ParticipantFilter filter, PageRequest page, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(page.Page, PageRequest.FirstPage);
        var pageSize = Math.Clamp(page.PageSize, 1, PageRequest.MaxPageSize);

        var query = Context.Participants.AsNoTracking().Where(x => x.SessionId == sessionId);

        if (!string.IsNullOrWhiteSpace(filter.Group))
        {
            var group = filter.Group.Trim();
            query = query.Where(x => x.Group == group);
        }

        if (!string.IsNullOrWhiteSpace(filter.School))
        {
            var school = filter.School.Trim();
            query = query.Where(x => x.School == school);
        }

        if (filter.NeedsAccessibleSeat is { } needsAccessible)
        {
            query = query.Where(x => x.NeedsAccessibleSeat == needsAccessible);
        }

        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var pattern = LikeWildcard + EscapeLike(page.Search.Trim()) + LikeWildcard;
            query = query.Where(x =>
                EF.Functions.ILike(x.Name.LastName, pattern, LikeEscape)
                || EF.Functions.ILike(x.Name.FirstName, pattern, LikeEscape)
                || (x.Name.MiddleName != null && EF.Functions.ILike(x.Name.MiddleName, pattern, LikeEscape))
                || (x.ExternalId != null && EF.Functions.ILike(x.ExternalId, pattern, LikeEscape))
                || (x.Group != null && EF.Functions.ILike(x.Group, pattern, LikeEscape))
                || (x.School != null && EF.Functions.ILike(x.School, pattern, LikeEscape)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await Sort(query, filter.SortBy, page.SortDirection)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Participant>(items, pageNumber, pageSize, total);
    }

    async Task<Option<Participant>> IParticipantQueries.GetById(ExamSessionId sessionId, ParticipantId id, CancellationToken cancellationToken)
    {
        var entity = await Context.Participants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.Id == id, cancellationToken);

        return entity ?? Option<Participant>.None;
    }

    public async Task<IReadOnlyList<Participant>> GetAllBySession(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        return await Context.Participants
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.Name.LastName)
            .ThenBy(x => x.Name.FirstName)
            .ThenBy(x => x.Name.MiddleName)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttributeSummary>> GetAttributeSummary(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        var rows = await Context.Participants
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .Select(x => new { x.Group, x.School, x.Attributes })
            .ToListAsync(cancellationToken);

        var summaries = new List<AttributeSummary>
        {
            Summarize(AttributeKeys.Group, rows.Select(x => x.Group).ToList()),
            Summarize(AttributeKeys.School, rows.Select(x => x.School).ToList()),
        };

        var customKeys = rows
            .SelectMany(x => x.Attributes.Keys)
            .Where(x => x is not AttributeKeys.Group and not AttributeKeys.School)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

        summaries.AddRange(customKeys.Select(key =>
            Summarize(key, rows.Select(x => x.Attributes.GetValueOrDefault(key)).ToList())));

        return summaries;
    }

    private static AttributeSummary Summarize(string key, IReadOnlyList<string?> values)
    {
        var present = values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToList();
        var byFrequency = present
            .GroupBy(x => x, StringComparer.Ordinal)
            .OrderByDescending(x => x.Count())
            .ThenBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => x.Key)
            .ToList();

        return new AttributeSummary(
            key,
            byFrequency.Count,
            values.Count - present.Count,
            byFrequency.Take(AttributeSummary.SampleSize).ToList());
    }

    private static IQueryable<Participant> Sort(IQueryable<Participant> query, ParticipantSortField sortBy, SortDirection direction)
    {
        var descending = direction == SortDirection.Descending;
        var ordered = sortBy switch
        {
            ParticipantSortField.FirstName => descending ? query.OrderByDescending(x => x.Name.FirstName) : query.OrderBy(x => x.Name.FirstName),
            ParticipantSortField.Group => descending ? query.OrderByDescending(x => x.Group) : query.OrderBy(x => x.Group),
            ParticipantSortField.School => descending ? query.OrderByDescending(x => x.School) : query.OrderBy(x => x.School),
            ParticipantSortField.ExternalId => descending ? query.OrderByDescending(x => x.ExternalId) : query.OrderBy(x => x.ExternalId),
            _ => descending ? query.OrderByDescending(x => x.Name.LastName) : query.OrderBy(x => x.Name.LastName),
        };

        return ordered
            .ThenBy(x => x.Name.LastName)
            .ThenBy(x => x.Name.FirstName)
            .ThenBy(x => x.Id);
    }

    private static string EscapeLike(string value)
        => value
            .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace(LikeAny.ToString(), LikeEscape + LikeAny, StringComparison.Ordinal)
            .Replace(LikeSingle.ToString(), LikeEscape + LikeSingle, StringComparison.Ordinal);
}