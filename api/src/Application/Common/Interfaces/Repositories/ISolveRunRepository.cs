using Domain.Sessions;
using Domain.SolveRuns;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface ISolveRunRepository
{
    Task<Option<SolveRun>> GetById(SolveRunId id, CancellationToken cancellationToken = default);

    Task<Option<SolveRun>> GetActive(ExamSessionId sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SolveRun>> GetAllActive(CancellationToken cancellationToken = default);

    Task<SolveRun> AddAsync(SolveRun entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyList<SolveRun> entities, bool saveChanges = true, CancellationToken cancellationToken = default);

    Task<SolveRun> UpdateAsync(SolveRun entity, CancellationToken cancellationToken = default);

    Task<SolveRun> RemoveAsync(SolveRun entity, CancellationToken cancellationToken = default);
}