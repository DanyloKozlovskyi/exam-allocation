using Application.Sessions.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class SessionErrorFactory
{
    public static ObjectResult ToObjectResult(this SessionException error)
    {
        var statusCode = error switch
        {
            SessionNotFoundException => StatusCodes.Status404NotFound,
            SessionAlreadyExistsException => StatusCodes.Status409Conflict,
            SessionConcurrencyException => StatusCodes.Status409Conflict,
            SessionArgumentException => StatusCodes.Status400BadRequest,
            SessionUnknownException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

        return ErrorResults.Problem(
            statusCode,
            error.Code,
            error.Message);
    }
}