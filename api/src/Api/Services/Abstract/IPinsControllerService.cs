using Api.Dtos;

namespace Api.Services.Abstract;

public interface IPinsControllerService
{
    Task<IReadOnlyList<PinDto>> GetAll(Guid sessionId, CancellationToken cancellationToken);
}