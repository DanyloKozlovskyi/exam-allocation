using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Centers;
using Domain.Sessions;
using LanguageExt;

namespace Infrastructure.Persistence.Repositories;

public class TestCenterRepository(ApplicationDbContext dbContext)
    : RepositoryBase<TestCenter, TestCenterId>(dbContext), ITestCenterRepository, ITestCenterQueries
{
    public Task<Option<TestCenter>> GetById(ExamSessionId sessionId, TestCenterId id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<TestCenter>> FindByName(ExamSessionId sessionId, string name, Option<TestCenterId> excludeId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<TestCenter>> GetAll(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}