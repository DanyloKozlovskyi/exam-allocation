using Api.Dtos;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Domain.Sessions;
using LanguageExt;

namespace Api.Services.Implementation;

public class SessionsControllerService(IExamSessionQueries sessionQueries) : ISessionsControllerService
{
    public async Task<IReadOnlyList<SessionSummaryDto>> GetAll(CancellationToken cancellationToken)
    {
        var sessions = await sessionQueries.GetAll(cancellationToken);
        return sessions.Select(SessionSummaryDto.FromDomain).ToList();
    }

    public async Task<Option<SessionDto>> Get(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await sessionQueries.GetById(new ExamSessionId(sessionId), cancellationToken);
        return session.Map(SessionDto.FromDomain);
    }

    public async Task<Option<SessionTotalsDto>> Totals(Guid sessionId, CancellationToken cancellationToken)
    {
        var totals = await sessionQueries.GetTotals(new ExamSessionId(sessionId), cancellationToken);

        // Stale detection against the latest solve run is wired in Phase 2 (SolveRuns).
        return totals.Map(x => SessionTotalsDto.FromModel(x, false));
    }
}