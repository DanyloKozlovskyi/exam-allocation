using Application.Pins.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class PinErrorFactory
{
    public static ObjectResult ToObjectResult(this PinException error)
    {
        var statusCode = error switch
        {
            PinSessionNotFoundException => StatusCodes.Status404NotFound,
            PinParticipantNotFoundException => StatusCodes.Status404NotFound,
            PinRoomNotFoundException => StatusCodes.Status404NotFound,
            PinSeatNotFoundException => StatusCodes.Status404NotFound,
            PinConflictException => StatusCodes.Status409Conflict,
            PinNotFoundException => StatusCodes.Status404NotFound,
            PinArgumentException => StatusCodes.Status400BadRequest,
            PinUnknownException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

        return ErrorResults.Problem(
            statusCode,
            error.Code,
            error.Message);
    }
}