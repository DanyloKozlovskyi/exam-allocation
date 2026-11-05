using Application.Allocations.Models;
using Application.Common.Interfaces;

namespace Infrastructure.ImportExport.Exporters;

public class XmlAllocationExporter : IAllocationExporter
{
    public ExportFormat Format => ExportFormat.Xml;

    public Task<ExportFile> Export(AllocationExportModel model, Guid? roomId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}