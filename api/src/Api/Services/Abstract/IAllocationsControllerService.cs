using Api.Dtos;
using Application.Common.Models;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IAllocationsControllerService
{
    Task<IReadOnlyList<AllocationSummaryDto>> GetByRun(Guid runId, CancellationToken cancellationToken);

    Task<Option<AllocationDto>> Get(Guid allocationId, CancellationToken cancellationToken);

    Task<Option<RoomSeatingDto>> RoomSeating(Guid allocationId, Guid roomId, CancellationToken cancellationToken);

    Task<Option<PagedDto<AllocationRowDto>>> Participants(Guid allocationId, PageRequest page, CancellationToken cancellationToken);
}