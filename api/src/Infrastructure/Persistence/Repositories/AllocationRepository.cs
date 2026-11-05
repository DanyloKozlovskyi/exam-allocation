using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Models;
using Domain.Allocations;
using Domain.Rooms;
using Domain.SolveRuns;
using LanguageExt;

namespace Infrastructure.Persistence.Repositories;

public class AllocationRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Allocation, AllocationId>(dbContext), IAllocationRepository, IAllocationQueries
{
    public Task<Option<Allocation>> GetById(AllocationId id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Allocation>> GetByRun(SolveRunId runId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<AllocationItem>> GetItemsByRoom(AllocationId id, RoomId roomId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<PagedResult<AllocationItem>> GetItemsPage(AllocationId id, PageRequest page, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}