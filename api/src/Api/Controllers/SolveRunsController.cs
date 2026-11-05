using Api.Constants;
using Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class SolveRunsController : ControllerBase
{
    [HttpGet(ApiRoutes.SolveRuns.List)]
    public Task<ActionResult<IReadOnlyList<SolveRunDto>>> GetAll(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<IReadOnlyList<SolveRunDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.SolveRuns.Create)]
    public Task<ActionResult<SolveRunDto>> Create(
        [FromRoute] Guid sessionId,
        [FromBody] SolveOptionsDto options,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<SolveRunDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet(ApiRoutes.SolveRuns.ById)]
    public Task<ActionResult<SolveRunDto>> Get(
        [FromRoute] Guid runId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<SolveRunDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.SolveRuns.Cancel)]
    public Task<ActionResult> Cancel(
        [FromRoute] Guid runId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.SolveRuns.Precheck)]
    public Task<ActionResult<IReadOnlyList<DiagnosticDto>>> Precheck(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<IReadOnlyList<DiagnosticDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }
}