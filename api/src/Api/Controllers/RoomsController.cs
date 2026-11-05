using Api.Constants;
using Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class RoomsController : ControllerBase
{
    [HttpGet(ApiRoutes.Rooms.List)]
    public Task<ActionResult<IReadOnlyList<RoomDto>>> GetAll(
        [FromRoute] Guid centerId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<IReadOnlyList<RoomDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet(ApiRoutes.Rooms.ById)]
    public Task<ActionResult<RoomDto>> Get(
        [FromRoute] Guid centerId,
        [FromRoute] Guid roomId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<RoomDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Rooms.Create)]
    public Task<ActionResult<RoomDto>> Create(
        [FromRoute] Guid centerId,
        [FromBody] RoomDto room,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<RoomDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Rooms.CreateBulk)]
    public Task<ActionResult<IReadOnlyList<RoomDto>>> CreateBulk(
        [FromRoute] Guid centerId,
        [FromBody] CreateRoomsBulkDto rooms,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<IReadOnlyList<RoomDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPut(ApiRoutes.Rooms.Edit)]
    public Task<ActionResult<RoomDto>> Edit(
        [FromRoute] Guid centerId,
        [FromBody] RoomDto room,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<RoomDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpDelete(ApiRoutes.Rooms.ById)]
    public Task<ActionResult> Delete(
        [FromRoute] Guid centerId,
        [FromRoute] Guid roomId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Rooms.PreviewLayout)]
    public Task<ActionResult<RoomLayoutPreviewDto>> PreviewLayout(
        [FromBody] RoomLayoutDto layout,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<RoomLayoutPreviewDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }
}