using Application.SolveRuns.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class SolveRunErrorFactory
{
    public static ObjectResult ToObjectResult(this SolveRunException error)
    {
        var statusCode = error switch
        {
            SolveRunSessionNotFoundException => StatusCodes.Status404NotFound,
            SolveRunNotFoundException => StatusCodes.Status404NotFound,
            SolveRunAlreadyActiveException => StatusCodes.Status409Conflict,
            SolveRunNotCancellableException => StatusCodes.Status409Conflict,
            SolveRunArgumentException => StatusCodes.Status400BadRequest,
            SolveRunUnknownException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

        return ErrorResults.Problem(
            statusCode,
            error.Code,
            error.Message);
    }
}