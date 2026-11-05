using Api.Constants;
using Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class SessionsController : ControllerBase
{
    [HttpGet(ApiRoutes.Sessions.List)]
    public Task<ActionResult<IReadOnlyList<SessionSummaryDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<IReadOnlyList<SessionSummaryDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet(ApiRoutes.Sessions.ById)]
    public Task<ActionResult<SessionDto>> Get(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<SessionDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet(ApiRoutes.Sessions.Totals)]
    public Task<ActionResult<SessionTotalsDto>> Totals(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<SessionTotalsDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Sessions.Create)]
    public Task<ActionResult<SessionDto>> Create(
        [FromBody] SessionDto session,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<SessionDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPut(ApiRoutes.Sessions.Edit)]
    public Task<ActionResult<SessionDto>> Edit(
        [FromBody] SessionDto session,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<SessionDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpDelete(ApiRoutes.Sessions.ById)]
    public Task<ActionResult> Delete(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }
}