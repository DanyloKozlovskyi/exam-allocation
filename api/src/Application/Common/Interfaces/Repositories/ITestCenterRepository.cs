using Domain.Centers;
using Domain.Sessions;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface ITestCenterRepository
{
    Task<Option<TestCenter>> GetById(ExamSessionId sessionId, TestCenterId id, CancellationToken cancellationToken = default);

    /// <summary>Tracked lookup by id only (callers that know the center but not its session).</summary>
    Task<Option<TestCenter>> GetById(TestCenterId id, CancellationToken cancellationToken = default);

    Task<Option<TestCenter>> FindByName(ExamSessionId sessionId, string name, Option<TestCenterId> excludeId, CancellationToken cancellationToken = default);

    Task<TestCenter> AddAsync(TestCenter entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyList<TestCenter> entities, bool saveChanges = true, CancellationToken cancellationToken = default);

    Task<TestCenter> UpdateAsync(TestCenter entity, CancellationToken cancellationToken = default);

    Task<TestCenter> RemoveAsync(TestCenter entity, CancellationToken cancellationToken = default);
}