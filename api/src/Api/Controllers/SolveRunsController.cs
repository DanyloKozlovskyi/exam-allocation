using Api.Constants;
using Api.Dtos;
using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.SolveRuns.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class SolveRunsController(ISender sender, ISolveRunsControllerService controllerService) : ControllerBase
{
    [HttpGet(ApiRoutes.SolveRuns.List)]
    public async Task<ActionResult<IReadOnlyList<SolveRunDto>>> GetAll(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var runs = await controllerService.GetAll(sessionId, cancellationToken);
        return Ok(runs);
    }

    [HttpPost(ApiRoutes.SolveRuns.Create)]
    public async Task<ActionResult<SolveRunDto>> Create(
        [FromRoute] Guid sessionId,
        [FromBody] SolveOptionsDto options,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartSolveRunCommand { SessionId = sessionId, Options = options.ToOptions() }, cancellationToken);
        return await result.MatchAsync<ActionResult<SolveRunDto>>(
            async run => (await controllerService.Get(run.Id.Value, cancellationToken)).Match<ActionResult<SolveRunDto>>(dto => dto, () => NotFound()),
            e => e.ToObjectResult());
    }

    [HttpGet(ApiRoutes.SolveRuns.ById)]
    public async Task<ActionResult<SolveRunDto>> Get(
        [FromRoute] Guid runId,
        CancellationToken cancellationToken)
    {
        var run = await controllerService.Get(runId, cancellationToken);
        return run.Match<ActionResult<SolveRunDto>>(dto => dto, () => NotFound());
    }

    [HttpPost(ApiRoutes.SolveRuns.Cancel)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Cancel(
        [FromRoute] Guid runId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelSolveRunCommand { RunId = runId }, cancellationToken);
        return result.Match<ActionResult>(_ => Accepted(), e => e.ToObjectResult());
    }

    [HttpPost(ApiRoutes.SolveRuns.Precheck)]
    public async Task<ActionResult<IReadOnlyList<DiagnosticDto>>> Precheck(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RunPrecheckCommand { SessionId = sessionId }, cancellationToken);
        return result.Match<ActionResult<IReadOnlyList<DiagnosticDto>>>(
            diagnostics => diagnostics.Select(DiagnosticDto.FromDomain).ToList(),
            e => e.ToObjectResult());
    }
}