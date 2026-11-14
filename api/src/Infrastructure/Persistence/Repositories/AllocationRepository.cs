using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Models;
using Domain.Allocations;
using Domain.Rooms;
using Domain.SolveRuns;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class AllocationRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Allocation, AllocationId>(dbContext), IAllocationRepository, IAllocationQueries
{
    public async Task<Option<Allocation>> GetById(AllocationId id, CancellationToken cancellationToken)
    {
        var entity = await Context.Allocations
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<Allocation>.None;
    }

    public async Task<IReadOnlyList<Allocation>> GetByRun(SolveRunId runId, CancellationToken cancellationToken)
    {
        return await Context.Allocations
            .AsNoTracking()
            .Where(x => x.RunId == runId)
            .OrderBy(x => x.Index)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AllocationItem>> GetItemsByRoom(AllocationId id, RoomId roomId, CancellationToken cancellationToken)
    {
        return await Context.AllocationItems
            .AsNoTracking()
            .Where(x => x.AllocationId == id && x.RoomId == roomId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<AllocationItem>> GetItemsPage(AllocationId id, PageRequest page, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(page.Page, PageRequest.FirstPage);
        var pageSize = Math.Clamp(page.PageSize, 1, PageRequest.MaxPageSize);
        var query = Context.AllocationItems.AsNoTracking().Where(x => x.AllocationId == id);
        var total = await query.CountAsync(cancellationToken);
        var ordered = page.SortDirection == SortDirection.Descending
            ? query.OrderByDescending(x => x.ParticipantId)
            : query.OrderBy(x => x.ParticipantId);
        var items = await ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<AllocationItem>(items, pageNumber, pageSize, total);
    }

    async Task<Option<Allocation>> IAllocationQueries.GetById(AllocationId id, CancellationToken cancellationToken)
    {
        var entity = await Context.Allocations
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<Allocation>.None;
    }
}