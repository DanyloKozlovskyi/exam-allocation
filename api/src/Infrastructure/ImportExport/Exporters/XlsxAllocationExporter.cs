using Application.Allocations.Models;
using Application.Common.Interfaces;

namespace Infrastructure.ImportExport.Exporters;

public class XlsxAllocationExporter : IAllocationExporter
{
    public ExportFormat Format => ExportFormat.Xlsx;

    public Task<ExportFile> Export(AllocationExportModel model, Guid? roomId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}