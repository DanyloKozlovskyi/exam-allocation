using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Centers;
using Domain.Sessions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class TestCenterRepository(ApplicationDbContext dbContext)
    : RepositoryBase<TestCenter, TestCenterId>(dbContext), ITestCenterRepository, ITestCenterQueries
{
    /// <summary>Tracked lookup within a session.</summary>
    public async Task<Option<TestCenter>> GetById(ExamSessionId sessionId, TestCenterId id, CancellationToken cancellationToken)
    {
        var entity = await Context.TestCenters
            .Include(x => x.Rooms)
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.Id == id, cancellationToken);

        return entity ?? Option<TestCenter>.None;
    }

    /// <summary>Tracked lookup by id only.</summary>
    public async Task<Option<TestCenter>> GetById(TestCenterId id, CancellationToken cancellationToken)
    {
        var entity = await Context.TestCenters
            .Include(x => x.Rooms)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity ?? Option<TestCenter>.None;
    }

    public async Task<Option<TestCenter>> FindByName(ExamSessionId sessionId, string name, Option<TestCenterId> excludeId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim();
        var query = Context.TestCenters.AsNoTracking().Where(x => x.SessionId == sessionId && x.Name == normalized);
        query = excludeId.Match(id => query.Where(x => x.Id != id), () => query);
        var entity = await query.FirstOrDefaultAsync(cancellationToken);

        return entity ?? Option<TestCenter>.None;
    }

    public async Task<IReadOnlyList<TestCenter>> GetAll(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        return await Context.TestCenters
            .AsNoTracking()
            .Include(x => x.Rooms)
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    async Task<Option<TestCenter>> ITestCenterQueries.GetById(ExamSessionId sessionId, TestCenterId id, CancellationToken cancellationToken)
    {
        var entity = await Context.TestCenters
            .AsNoTracking()
            .Include(x => x.Rooms)
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.Id == id, cancellationToken);

        return entity ?? Option<TestCenter>.None;
    }
}