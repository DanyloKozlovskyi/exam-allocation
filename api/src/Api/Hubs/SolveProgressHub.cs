using Api.Constants;
using Api.Services.Abstract;
using Domain.SolveRuns;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

/// <summary>
/// Clients subscribe to a run's group. On subscribe the hub replays the last progress event and,
/// if the run has already finished, sends <see cref="SolveProgressHubMethods.RunCompleted"/> immediately.
/// </summary>
public class SolveProgressHub(SolveProgressStore store, ISolveRunsControllerService runs) : Hub
{
    public async Task SubscribeToRun(Guid runId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Run(runId));

        if (store.Get(runId) is { } progress)
        {
            await Clients.Caller.SendAsync(SolveProgressHubMethods.ProgressUpdated, progress);
        }

        var run = await runs.Get(runId, Context.ConnectionAborted);
        await run.IfSomeAsync(async dto =>
        {
            if (dto.Status is not (SolveRunStatus.Queued or SolveRunStatus.Running))
            {
                await Clients.Caller.SendAsync(SolveProgressHubMethods.RunCompleted, dto);
            }
        });
    }

    public Task UnsubscribeFromRun(Guid runId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Run(runId));
}