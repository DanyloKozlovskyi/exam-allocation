using Application.Allocations.Models;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IExportsControllerService
{
    Task<Option<AllocationExportModel>> GetModel(Guid allocationId, CancellationToken cancellationToken);

    Task<ExportFile> Export(AllocationExportModel model, ExportFormat format, Guid? roomId, CancellationToken cancellationToken);
}