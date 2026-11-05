using Api.Dtos;
using Api.Services.Abstract;

namespace Api.Services.Implementation;

public class PinsControllerService : IPinsControllerService
{
    public Task<IReadOnlyList<PinDto>> GetAll(Guid sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}