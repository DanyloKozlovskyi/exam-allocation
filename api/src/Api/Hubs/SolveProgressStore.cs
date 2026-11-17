using System.Collections.Concurrent;
using Api.Dtos;

namespace Api.Hubs;

/// <summary>Last progress event per active run, replayed to clients that subscribe late.</summary>
public class SolveProgressStore
{
    private readonly ConcurrentDictionary<Guid, SolveProgressDto> lastProgress = new();

    public void Set(SolveProgressDto progress) => lastProgress[progress.RunId] = progress;

    public SolveProgressDto? Get(Guid runId) => lastProgress.TryGetValue(runId, out var progress) ? progress : null;

    public void Remove(Guid runId) => lastProgress.TryRemove(runId, out _);
}