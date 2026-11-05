using Application.Allocations.Models;
using Application.Common.Interfaces;

namespace Infrastructure.ImportExport.Exporters;

public class CsvAllocationExporter : IAllocationExporter
{
    public ExportFormat Format => ExportFormat.Csv;

    public Task<ExportFile> Export(AllocationExportModel model, Guid? roomId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}