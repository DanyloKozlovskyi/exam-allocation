using Api.Dtos;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Solving.Services;
using Domain.Sessions;
using Domain.SolveRuns;
using LanguageExt;

namespace Api.Services.Implementation;

public class SessionsControllerService(
    IExamSessionQueries sessionQueries,
    ISolveRunQueries runQueries,
    ISessionFingerprint fingerprint) : ISessionsControllerService
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

        if (totals.IsNone)
        {
            return Option<SessionTotalsDto>.None;
        }

        var isStale = await IsStale(new ExamSessionId(sessionId), cancellationToken);
        return totals.Map(x => SessionTotalsDto.FromModel(x, isStale));
    }

    /// <summary>Stale when the latest succeeded run was computed from different solver-relevant data.</summary>
    private async Task<bool> IsStale(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        var runs = await runQueries.GetAll(sessionId, cancellationToken);
        var latest = runs.FirstOrDefault(x => x.Status == SolveRunStatus.Succeeded);
        if (latest is null)
        {
            return false;
        }

        var current = await fingerprint.Compute(sessionId, cancellationToken);
        return !string.Equals(latest.InputHash, current, StringComparison.Ordinal);
    }
}