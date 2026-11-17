using Api.Dtos;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Solving.Services;
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

        var currentHash = await fingerprint.Compute(id, cancellationToken);
        var result = new List<SolveRunDto>(runs.Count);
        foreach (var run in runs)
        {
            result.Add(await ToDto(run, currentHash, cancellationToken));
        }

        return result;
    }

    public async Task<Option<SolveRunDto>> Get(Guid runId, CancellationToken cancellationToken)
    {
        var run = await runQueries.GetById(new SolveRunId(runId), cancellationToken);
        if (run.IsNone)
        {
            return Option<SolveRunDto>.None;
        }

        var found = run.Match(x => x, () => throw new InvalidOperationException());
        var currentHash = await fingerprint.Compute(found.SessionId, cancellationToken);
        return await ToDto(found, currentHash, cancellationToken);
    }

    private async Task<SolveRunDto> ToDto(SolveRun run, string currentHash, CancellationToken cancellationToken)
    {
        var allocations = await allocationQueries.GetByRun(run.Id, cancellationToken);
        var isStale = !string.Equals(run.InputHash, currentHash, StringComparison.Ordinal);
        return SolveRunDto.FromDomain(run, allocations.Select(x => x.Id.Value).ToList(), isStale);
    }
}