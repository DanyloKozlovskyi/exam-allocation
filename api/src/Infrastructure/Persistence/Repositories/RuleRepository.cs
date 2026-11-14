using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Rules;
using Domain.Sessions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Rule = Domain.Rules.Rule;

namespace Infrastructure.Persistence.Repositories;

public class RuleRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Rule, RuleId>(dbContext), IRuleRepository, IRuleQueries
{
    public async Task<Option<Rule>> GetById(ExamSessionId sessionId, RuleId id, CancellationToken cancellationToken)
    {
        var entity = await Context.Rules
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.Id == id, cancellationToken);

        return entity ?? Option<Rule>.None;
    }

    /// <summary>Rules of a session ordered by type name (stored as string), then by id — deterministic.</summary>
    public async Task<IReadOnlyList<Rule>> GetAll(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        return await Context.Rules
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    async Task<Option<Rule>> IRuleQueries.GetById(ExamSessionId sessionId, RuleId id, CancellationToken cancellationToken)
    {
        var entity = await Context.Rules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.Id == id, cancellationToken);

        return entity ?? Option<Rule>.None;
    }
}