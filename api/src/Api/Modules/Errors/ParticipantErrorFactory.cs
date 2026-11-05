using Application.Participants.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class ParticipantErrorFactory
{
    public static ObjectResult ToObjectResult(this ParticipantException error)
    {
        var statusCode = error switch
        {
            ParticipantSessionNotFoundException => StatusCodes.Status404NotFound,
            ParticipantNotFoundException => StatusCodes.Status404NotFound,
            ParticipantAlreadyExistsException => StatusCodes.Status409Conflict,
            ParticipantConcurrencyException => StatusCodes.Status409Conflict,
            ParticipantArgumentException => StatusCodes.Status400BadRequest,
            ParticipantUnknownException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

        return ErrorResults.Problem(
            statusCode,
            error.Code,
            error.Message);
    }
}