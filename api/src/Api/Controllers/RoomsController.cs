using Api.Constants;
using Api.Dtos;
using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Rooms.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class RoomsController(ISender sender, IRoomsControllerService controllerService) : ControllerBase
{
    [HttpGet(ApiRoutes.Rooms.List)]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetAll(
        [FromRoute] Guid centerId,
        CancellationToken cancellationToken)
    {
        var rooms = await controllerService.GetAll(centerId, cancellationToken);
        return Ok(rooms);
    }

    [HttpGet(ApiRoutes.Rooms.ById)]
    public async Task<ActionResult<RoomDto>> Get(
        [FromRoute] Guid centerId,
        [FromRoute] Guid roomId,
        CancellationToken cancellationToken)
    {
        var room = await controllerService.Get(centerId, roomId, cancellationToken);
        return room.Match<ActionResult<RoomDto>>(r => r, () => NotFound());
    }

    [HttpPost(ApiRoutes.Rooms.Create)]
    public async Task<ActionResult<RoomDto>> Create(
        [FromRoute] Guid centerId,
        [FromBody] RoomDto room,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(room.ToCreateCommand(centerId), cancellationToken);
        return result.Match<ActionResult<RoomDto>>(r => RoomDto.FromDomain(r, true), e => e.ToObjectResult());
    }

    [HttpPost(ApiRoutes.Rooms.CreateBulk)]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> CreateBulk(
        [FromRoute] Guid centerId,
        [FromBody] CreateRoomsBulkDto rooms,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(rooms.ToCommand(centerId), cancellationToken);
        return result.Match<ActionResult<IReadOnlyList<RoomDto>>>(
            r => r.Select(x => RoomDto.FromDomain(x, false)).ToList(),
            e => e.ToObjectResult());
    }

    [HttpPut(ApiRoutes.Rooms.Edit)]
    public async Task<ActionResult<RoomDto>> Edit(
        [FromRoute] Guid centerId,
        [FromBody] RoomDto room,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(room.ToUpdateCommand(centerId), cancellationToken);
        return result.Match<ActionResult<RoomDto>>(r => RoomDto.FromDomain(r, true), e => e.ToObjectResult());
    }

    [HttpDelete(ApiRoutes.Rooms.ById)]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid centerId,
        [FromRoute] Guid roomId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteRoomCommand { CenterId = centerId, Id = roomId }, cancellationToken);
        return result.Match<ActionResult>(_ => NoContent(), e => e.ToObjectResult());
    }

    [HttpPost(ApiRoutes.Rooms.PreviewLayout)]
    public ActionResult<RoomLayoutPreviewDto> PreviewLayout(
        [FromBody] RoomLayoutDto layout)
    {
        return controllerService.PreviewLayout(layout);
    }
}