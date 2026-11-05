using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Sessions.Models;
using Domain.Sessions;
using LanguageExt;

namespace Infrastructure.Persistence.Repositories;

public class ExamSessionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<ExamSession, ExamSessionId>(dbContext), IExamSessionRepository, IExamSessionQueries
{
    public Task<Option<ExamSession>> GetById(ExamSessionId id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<ExamSession>> FindByName(string name, Option<ExamSessionId> excludeId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ExamSession>> GetAll(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<SessionTotals>> GetTotals(ExamSessionId id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}