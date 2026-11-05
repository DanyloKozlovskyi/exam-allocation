using Application.Centers.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class TestCenterErrorFactory
{
    public static ObjectResult ToObjectResult(this TestCenterException error)
    {
        var statusCode = error switch
        {
            TestCenterSessionNotFoundException => StatusCodes.Status404NotFound,
            TestCenterNotFoundException => StatusCodes.Status404NotFound,
            TestCenterAlreadyExistsException => StatusCodes.Status409Conflict,
            TestCenterConcurrencyException => StatusCodes.Status409Conflict,
            TestCenterArgumentException => StatusCodes.Status400BadRequest,
            TestCenterUnknownException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

        return ErrorResults.Problem(
            statusCode,
            error.Code,
            error.Message);
    }
}