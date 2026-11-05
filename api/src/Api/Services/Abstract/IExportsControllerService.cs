using Api.Dtos;
using Application.Allocations.Models;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IExportsControllerService
{
    Task<Option<ExportFile>> Export(Guid allocationId, ExportFormat format, Guid? roomId, CancellationToken cancellationToken);
}