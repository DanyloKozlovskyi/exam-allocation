using Domain.Rules;
using Domain.Sessions;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IRuleRepository
{
    Task<Option<Rule>> GetById(ExamSessionId sessionId, RuleId id, CancellationToken cancellationToken = default);

    Task<Rule> AddAsync(Rule entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyList<Rule> entities, bool saveChanges = true, CancellationToken cancellationToken = default);

    Task<Rule> UpdateAsync(Rule entity, CancellationToken cancellationToken = default);

    Task<Rule> RemoveAsync(Rule entity, CancellationToken cancellationToken = default);
}