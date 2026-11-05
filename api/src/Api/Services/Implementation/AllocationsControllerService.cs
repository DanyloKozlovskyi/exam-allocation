using Api.Dtos;
using Api.Services.Abstract;
using Application.Common.Models;
using LanguageExt;

namespace Api.Services.Implementation;

public class AllocationsControllerService : IAllocationsControllerService
{
    public Task<IReadOnlyList<AllocationSummaryDto>> GetByRun(Guid runId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<AllocationDto>> Get(Guid allocationId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<RoomSeatingDto>> RoomSeating(Guid allocationId, Guid roomId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<PagedDto<AllocationRowDto>> Participants(Guid allocationId, PageRequest page, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}