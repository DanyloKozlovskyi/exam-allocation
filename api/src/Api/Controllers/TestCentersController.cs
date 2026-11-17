using Api.Constants;
using Api.Dtos;
using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Centers.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class TestCentersController(ISender sender, ITestCentersControllerService controllerService) : ControllerBase
{
    [HttpGet(ApiRoutes.Centers.List)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TestCenterDto>>> GetAll(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var centers = await controllerService.GetAll(sessionId, cancellationToken);
        return Ok(centers);
    }

    [HttpGet(ApiRoutes.Centers.ById)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestCenterDto>> Get(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid centerId,
        CancellationToken cancellationToken)
    {
        var center = await controllerService.Get(sessionId, centerId, cancellationToken);
        return center.Match<ActionResult<TestCenterDto>>(c => c, () => NotFound());
    }

    [HttpPost(ApiRoutes.Centers.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TestCenterDto>> Create(
        [FromRoute] Guid sessionId,
        [FromBody] TestCenterDto center,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(center.ToCreateCommand(sessionId), cancellationToken);
        return result.Match<ActionResult<TestCenterDto>>(c => TestCenterDto.FromDomain(c), e => e.ToObjectResult());
    }

    [HttpPut(ApiRoutes.Centers.Edit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TestCenterDto>> Edit(
        [FromRoute] Guid sessionId,
        [FromBody] TestCenterDto center,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(center.ToUpdateCommand(sessionId), cancellationToken);
        return result.Match<ActionResult<TestCenterDto>>(c => TestCenterDto.FromDomain(c), e => e.ToObjectResult());
    }

    [HttpDelete(ApiRoutes.Centers.ById)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid centerId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteTestCenterCommand { SessionId = sessionId, Id = centerId }, cancellationToken);
        return result.Match<ActionResult>(_ => NoContent(), e => e.ToObjectResult());
    }
}