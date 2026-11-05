using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Rules;
using Domain.Sessions;
using LanguageExt;
using Rule = Domain.Rules.Rule;

namespace Infrastructure.Persistence.Repositories;

public class RuleRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Rule, RuleId>(dbContext), IRuleRepository, IRuleQueries
{
    public Task<Option<Rule>> GetById(ExamSessionId sessionId, RuleId id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Rule>> GetAll(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}