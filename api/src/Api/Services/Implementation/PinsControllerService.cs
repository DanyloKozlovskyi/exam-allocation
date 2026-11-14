using Api.Dtos;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Domain.Sessions;

namespace Api.Services.Implementation;

public class PinsControllerService(IPinQueries pinQueries) : IPinsControllerService
{
    public async Task<IReadOnlyList<PinDto>> GetAll(Guid sessionId, CancellationToken cancellationToken)
    {
        var pins = await pinQueries.GetAll(new ExamSessionId(sessionId), cancellationToken);
        return pins.Select(x => x.ToDto(x.Seat?.Key.Value)).ToList();
    }
}