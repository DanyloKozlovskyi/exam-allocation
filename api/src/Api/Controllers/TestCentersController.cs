using Api.Constants;
using Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class TestCentersController : ControllerBase
{
    [HttpGet(ApiRoutes.Centers.List)]
    public Task<ActionResult<IReadOnlyList<TestCenterDto>>> GetAll(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<IReadOnlyList<TestCenterDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet(ApiRoutes.Centers.ById)]
    public Task<ActionResult<TestCenterDto>> Get(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid centerId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<TestCenterDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Centers.Create)]
    public Task<ActionResult<TestCenterDto>> Create(
        [FromRoute] Guid sessionId,
        [FromBody] TestCenterDto center,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<TestCenterDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPut(ApiRoutes.Centers.Edit)]
    public Task<ActionResult<TestCenterDto>> Edit(
        [FromRoute] Guid sessionId,
        [FromBody] TestCenterDto center,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<TestCenterDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpDelete(ApiRoutes.Centers.ById)]
    public Task<ActionResult> Delete(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid centerId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }
}