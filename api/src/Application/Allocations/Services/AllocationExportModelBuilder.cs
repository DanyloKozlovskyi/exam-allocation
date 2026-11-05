using Application.Allocations.Models;
using Domain.Allocations;
using LanguageExt;

namespace Application.Allocations.Services;

public class AllocationExportModelBuilder : IAllocationExportModelBuilder
{
    public Task<Option<AllocationExportModel>> Build(AllocationId allocationId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}