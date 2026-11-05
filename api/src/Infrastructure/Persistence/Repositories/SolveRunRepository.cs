using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Sessions;
using Domain.SolveRuns;
using LanguageExt;

namespace Infrastructure.Persistence.Repositories;

public class SolveRunRepository(ApplicationDbContext dbContext)
    : RepositoryBase<SolveRun, SolveRunId>(dbContext), ISolveRunRepository, ISolveRunQueries
{
    public Task<Option<SolveRun>> GetById(SolveRunId id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<SolveRun>> GetActive(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<SolveRun>> GetAllActive(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<SolveRun>> GetAll(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}