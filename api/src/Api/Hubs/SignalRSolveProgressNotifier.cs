using Api.Constants;
using Api.Dtos;
using Api.Services.Abstract;
using Application.Common.Interfaces;
using Domain.SolveRuns;
using Microsoft.AspNetCore.SignalR;
using Solver.Model;

namespace Api.Hubs;

public class SignalRSolveProgressNotifier(
    IHubContext<SolveProgressHub> hubContext,
    SolveProgressStore store,
    IServiceScopeFactory scopeFactory) : ISolveProgressNotifier
{
    public Task NotifyProgress(SolveRunId runId, SolveProgress progress, CancellationToken cancellationToken)
    {
        var dto = SolveProgressDto.FromModel(runId.Value, progress);
        store.Set(dto);
        return hubContext.Clients.Group(HubGroups.Run(runId.Value)).SendAsync(SolveProgressHubMethods.ProgressUpdated, dto, cancellationToken);
    }

    public async Task NotifyCompleted(SolveRun run, CancellationToken cancellationToken)
    {
        store.Remove(run.Id.Value);
        using var scope = scopeFactory.CreateScope();
        var runs = scope.ServiceProvider.GetRequiredService<ISolveRunsControllerService>();
        var dto = await runs.Get(run.Id.Value, cancellationToken);
        await dto.IfSomeAsync(value => hubContext.Clients.Group(HubGroups.Run(run.Id.Value)).SendAsync(SolveProgressHubMethods.RunCompleted, value, cancellationToken));
    }
}