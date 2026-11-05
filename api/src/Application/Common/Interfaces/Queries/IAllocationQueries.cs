using Application.Common.Models;
using Domain.Allocations;
using Domain.Rooms;
using Domain.SolveRuns;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IAllocationQueries
{
    Task<IReadOnlyList<Allocation>> GetByRun(SolveRunId runId, CancellationToken cancellationToken = default);

    Task<Option<Allocation>> GetById(AllocationId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AllocationItem>> GetItemsByRoom(AllocationId id, RoomId roomId, CancellationToken cancellationToken = default);

    Task<PagedResult<AllocationItem>> GetItemsPage(AllocationId id, PageRequest page, CancellationToken cancellationToken = default);
}