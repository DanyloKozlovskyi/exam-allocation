using Domain.Sessions;
using Domain.SolveRuns;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface ISolveRunQueries
{
    Task<IReadOnlyList<SolveRun>> GetAll(ExamSessionId sessionId, CancellationToken cancellationToken = default);

    Task<Option<SolveRun>> GetById(SolveRunId id, CancellationToken cancellationToken = default);
}