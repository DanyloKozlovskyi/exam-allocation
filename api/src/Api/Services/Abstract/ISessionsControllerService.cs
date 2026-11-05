using Api.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface ISessionsControllerService
{
    Task<IReadOnlyList<SessionSummaryDto>> GetAll(CancellationToken cancellationToken);

    Task<Option<SessionDto>> Get(Guid sessionId, CancellationToken cancellationToken);

    Task<Option<SessionTotalsDto>> Totals(Guid sessionId, CancellationToken cancellationToken);
}