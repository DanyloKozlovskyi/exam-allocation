using Api.Services.Abstract;
using Application.Allocations.Models;
using Application.Allocations.Services;
using Application.Common.Interfaces;
using Domain.Allocations;
using LanguageExt;

namespace Api.Services.Implementation;

public class ExportsControllerService(
    IAllocationExportModelBuilder modelBuilder,
    IEnumerable<IAllocationExporter> exporters)
    : IExportsControllerService
{
    public Task<Option<AllocationExportModel>> GetModel(Guid allocationId, CancellationToken cancellationToken)
        => modelBuilder.Build(new AllocationId(allocationId), cancellationToken);

    public Task<ExportFile> Export(AllocationExportModel model, ExportFormat format, Guid? roomId, CancellationToken cancellationToken)
    {
        var exporter = exporters.FirstOrDefault(x => x.Format == format)
            ?? throw new NotSupportedException($"No exporter registered for format {format}");
        return exporter.Export(model, roomId, cancellationToken);
    }
}