using Application.Allocations.Models;

namespace Application.Common.Interfaces;

/// <summary>One implementation per <see cref="ExportFormat"/>.</summary>
public interface IAllocationExporter
{
    ExportFormat Format { get; }

    Task<ExportFile> Export(AllocationExportModel model, Guid? roomId, CancellationToken cancellationToken);
}