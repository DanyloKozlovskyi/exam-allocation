using Application.Allocations.Models;
using Domain.Allocations;
using LanguageExt;

namespace Application.Allocations.Services;

public interface IAllocationExportModelBuilder
{
    Task<Option<AllocationExportModel>> Build(AllocationId allocationId, CancellationToken cancellationToken);
}