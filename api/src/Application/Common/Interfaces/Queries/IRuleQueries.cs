using Domain.Rules;
using Domain.Sessions;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IRuleQueries
{
    Task<IReadOnlyList<Rule>> GetAll(ExamSessionId sessionId, CancellationToken cancellationToken = default);

    Task<Option<Rule>> GetById(ExamSessionId sessionId, RuleId id, CancellationToken cancellationToken = default);
}