using Api.Dtos;
using Api.Services.Abstract;
using LanguageExt;

namespace Api.Services.Implementation;

public class SessionsControllerService : ISessionsControllerService
{
    public Task<IReadOnlyList<SessionSummaryDto>> GetAll(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<SessionDto>> Get(Guid sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<SessionTotalsDto>> Totals(Guid sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}