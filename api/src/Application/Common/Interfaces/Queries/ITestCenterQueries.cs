using Domain.Centers;
using Domain.Sessions;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface ITestCenterQueries
{
    Task<IReadOnlyList<TestCenter>> GetAll(ExamSessionId sessionId, CancellationToken cancellationToken = default);

    Task<Option<TestCenter>> GetById(ExamSessionId sessionId, TestCenterId id, CancellationToken cancellationToken = default);
}