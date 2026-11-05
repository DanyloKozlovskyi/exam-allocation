using Api.Constants;
using Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class PinsController : ControllerBase
{
    [HttpGet(ApiRoutes.Pins.List)]
    public Task<ActionResult<IReadOnlyList<PinDto>>> GetAll(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<IReadOnlyList<PinDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Pins.Create)]
    public Task<ActionResult<PinDto>> Upsert(
        [FromRoute] Guid sessionId,
        [FromBody] PinDto pin,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<PinDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpDelete(ApiRoutes.Pins.ByParticipant)]
    public Task<ActionResult> Delete(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid participantId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Pins.PinAll)]
    public Task<ActionResult<IReadOnlyList<PinDto>>> PinAll(
        [FromRoute] Guid allocationId,
        [FromBody] PinAllDto request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<IReadOnlyList<PinDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }
}