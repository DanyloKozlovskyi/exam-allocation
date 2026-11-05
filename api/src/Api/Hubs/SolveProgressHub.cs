using Api.Constants;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

/// <summary>Clients subscribe to a run's group; last-progress replay is implemented in Phase 2.</summary>
public class SolveProgressHub : Hub
{
    public Task SubscribeToRun(Guid runId)
        => Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Run(runId));

    public Task UnsubscribeFromRun(Guid runId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Run(runId));
}