using Application.Common.Models;
using Domain.Allocations;
using Domain.Rooms;
using Domain.SolveRuns;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IAllocationQueries
{
    Task<IReadOnlyList<Allocation>> GetByRun(SolveRunId runId, CancellationToken cancellationToken = default);

    /// <summary>Allocation ids per run in <see cref="GetByRun"/> order, in one query (ids only).</summary>
    Task<IReadOnlyDictionary<SolveRunId, IReadOnlyList<AllocationId>>> GetIdsByRuns(IReadOnlyCollection<SolveRunId> runIds, CancellationToken cancellationToken = default);

    Task<Option<Allocation>> GetById(AllocationId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AllocationItem>> GetItemsByRoom(AllocationId id, RoomId roomId, CancellationToken cancellationToken = default);

    Task<PagedResult<AllocationItem>> GetItemsPage(AllocationId id, PageRequest page, CancellationToken cancellationToken = default);
}