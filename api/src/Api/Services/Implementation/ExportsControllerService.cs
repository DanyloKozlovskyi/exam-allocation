using Api.Dtos;
using Api.Services.Abstract;
using Application.Allocations.Models;
using LanguageExt;

namespace Api.Services.Implementation;

public class ExportsControllerService : IExportsControllerService
{
    public Task<Option<ExportFile>> Export(Guid allocationId, ExportFormat format, Guid? roomId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}