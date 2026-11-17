using Api.Constants;
using Api.Dtos;
using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Allocations.Commands;
using Application.Pins.Commands;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class PinsController(ISender sender, IPinsControllerService controllerService) : ControllerBase
{
    [HttpGet(ApiRoutes.Pins.List)]
    public async Task<ActionResult<IReadOnlyList<PinDto>>> GetAll(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var pins = await controllerService.GetAll(sessionId, cancellationToken);
        return Ok(pins);
    }

    [HttpPost(ApiRoutes.Pins.Create)]
    public async Task<ActionResult<PinDto>> Upsert(
        [FromRoute] Guid sessionId,
        [FromBody] PinDto pin,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(pin.ToUpsertCommand(sessionId), cancellationToken);
        return result.Match<ActionResult<PinDto>>(
            p => p.ToDto(p.SeatId is null ? null : p.Seat?.Key.Value ?? SeatKey.Parse(pin.SeatKey!.Trim()).Value),
            e => e.ToObjectResult());
    }

    [HttpDelete(ApiRoutes.Pins.ByParticipant)]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid participantId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeletePinCommand { SessionId = sessionId, ParticipantId = participantId }, cancellationToken);
        return result.Match<ActionResult>(_ => NoContent(), e => e.ToObjectResult());
    }

    /// <summary>Pins every participant of an allocation (or of the given rooms) to their current room and seat.</summary>
    [HttpPost(ApiRoutes.Pins.PinAll)]
    public async Task<ActionResult<IReadOnlyList<PinDto>>> PinAll(
        [FromRoute] Guid allocationId,
        [FromBody] PinAllDto request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PinAllCommand { AllocationId = allocationId, RoomIds = request.RoomIds }, cancellationToken);
        return result.Match<ActionResult<IReadOnlyList<PinDto>>>(
            pins => pins.Select(x => x.Pin.ToDto(x.SeatKey)).ToList(),
            e => e.ToObjectResult());
    }
}