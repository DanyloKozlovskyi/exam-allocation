using Application.Allocations.Models;
using Application.Common.Interfaces;

namespace Infrastructure.ImportExport.Exporters;

public class PdfAllocationExporter : IAllocationExporter
{
    public ExportFormat Format => ExportFormat.Pdf;

    public Task<ExportFile> Export(AllocationExportModel model, Guid? roomId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}