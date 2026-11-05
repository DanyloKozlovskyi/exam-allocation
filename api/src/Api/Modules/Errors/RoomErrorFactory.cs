using Application.Rooms.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class RoomErrorFactory
{
    public static ObjectResult ToObjectResult(this RoomException error)
    {
        var statusCode = error switch
        {
            RoomCenterNotFoundException => StatusCodes.Status404NotFound,
            RoomNotFoundException => StatusCodes.Status404NotFound,
            RoomAlreadyExistsException => StatusCodes.Status409Conflict,
            RoomSeatReferencedByPinException => StatusCodes.Status409Conflict,
            RoomConcurrencyException => StatusCodes.Status409Conflict,
            RoomArgumentException => StatusCodes.Status400BadRequest,
            RoomUnknownException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

        return ErrorResults.Problem(
            statusCode,
            error.Code,
            error.Message,
            error is RoomSeatReferencedByPinException pinned ? pinned.PinIds : null);
    }
}