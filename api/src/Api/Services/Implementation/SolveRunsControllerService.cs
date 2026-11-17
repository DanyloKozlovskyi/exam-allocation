using Api.Dtos;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Solving.Services;
using Domain.Allocations;
using Domain.Sessions;
using Domain.SolveRuns;
using LanguageExt;

namespace Api.Services.Implementation;

public class SolveRunsControllerService(
    ISolveRunQueries runQueries,
    IAllocationQueries allocationQueries,
    ISessionFingerprint fingerprint) : ISolveRunsControllerService
{
    public async Task<IReadOnlyList<SolveRunDto>> GetAll(Guid sessionId, CancellationToken cancellationToken)
    {
        var id = new ExamSessionId(sessionId);
        var runs = await runQueries.GetAll(id, cancellationToken);
        if (runs.Count == 0)
        {
            return [];
        }

        var allocationIds = await allocationQueries.GetIdsByRuns(runs.Select(x => x.Id).ToList(), cancellationToken);
        var currentHash = runs.Any(x => !x.IsActive) ? await fingerprint.Compute(id, cancellationToken) : null;
        return runs.Select(run => ToDto(run, allocationIds, currentHash)).ToList();
    }

    public async Task<Option<SolveRunDto>> Get(Guid runId, CancellationToken cancellationToken)
    {
        var run = await runQueries.GetById(new SolveRunId(runId), cancellationToken);
        if (run.IsNone)
        {
            return Option<SolveRunDto>.None;
        }

        var found = run.Match(x => x, () => throw new InvalidOperationException());
        var allocationIds = await allocationQueries.GetIdsByRuns([found.Id], cancellationToken);

        // Active runs are polled every few seconds; staleness only matters for finished results (docs/05).
        var currentHash = found.IsActive ? null : await fingerprint.Compute(found.SessionId, cancellationToken);
        return ToDto(found, allocationIds, currentHash);
    }

    private static SolveRunDto ToDto(SolveRun run, IReadOnlyDictionary<SolveRunId, IReadOnlyList<AllocationId>> allocationIds, string? currentHash)
    {
        var ids = allocationIds.TryGetValue(run.Id, out var found) ? found.Select(x => x.Value).ToList() : [];
        var isStale = currentHash is not null && !run.IsActive && !string.Equals(run.InputHash, currentHash, StringComparison.Ordinal);
        return SolveRunDto.FromDomain(run, ids, isStale);
    }
}