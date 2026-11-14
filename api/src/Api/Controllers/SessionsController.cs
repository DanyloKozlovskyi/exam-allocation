using Api.Constants;
using Api.Dtos;
using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Sessions.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class SessionsController(ISender sender, ISessionsControllerService controllerService) : ControllerBase
{
    [HttpGet(ApiRoutes.Sessions.List)]
    public async Task<ActionResult<IReadOnlyList<SessionSummaryDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var sessions = await controllerService.GetAll(cancellationToken);
        return Ok(sessions);
    }

    [HttpGet(ApiRoutes.Sessions.ById)]
    public async Task<ActionResult<SessionDto>> Get(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await controllerService.Get(sessionId, cancellationToken);
        return session.Match<ActionResult<SessionDto>>(s => s, () => NotFound());
    }

    [HttpGet(ApiRoutes.Sessions.Totals)]
    public async Task<ActionResult<SessionTotalsDto>> Totals(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var totals = await controllerService.Totals(sessionId, cancellationToken);
        return totals.Match<ActionResult<SessionTotalsDto>>(t => t, () => NotFound());
    }

    [HttpPost(ApiRoutes.Sessions.Create)]
    public async Task<ActionResult<SessionDto>> Create(
        [FromBody] SessionDto session,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(session.ToCreateCommand(), cancellationToken);
        return result.Match<ActionResult<SessionDto>>(s => SessionDto.FromDomain(s), e => e.ToObjectResult());
    }

    [HttpPut(ApiRoutes.Sessions.Edit)]
    public async Task<ActionResult<SessionDto>> Edit(
        [FromBody] SessionDto session,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(session.ToUpdateCommand(), cancellationToken);
        return result.Match<ActionResult<SessionDto>>(s => SessionDto.FromDomain(s), e => e.ToObjectResult());
    }

    [HttpDelete(ApiRoutes.Sessions.ById)]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteSessionCommand { Id = sessionId }, cancellationToken);
        return result.Match<ActionResult>(_ => NoContent(), e => e.ToObjectResult());
    }
}