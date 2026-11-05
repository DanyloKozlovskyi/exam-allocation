using Application.Common.Models;
using Domain.Allocations;
using Domain.Rooms;
using Domain.SolveRuns;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IAllocationRepository
{
    Task<Option<Allocation>> GetById(AllocationId id, CancellationToken cancellationToken = default);

    Task<Allocation> AddAsync(Allocation entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyList<Allocation> entities, bool saveChanges = true, CancellationToken cancellationToken = default);

    Task<Allocation> UpdateAsync(Allocation entity, CancellationToken cancellationToken = default);

    Task<Allocation> RemoveAsync(Allocation entity, CancellationToken cancellationToken = default);
}