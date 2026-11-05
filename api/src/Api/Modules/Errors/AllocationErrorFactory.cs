using Application.Allocations.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class AllocationErrorFactory
{
    public static ObjectResult ToObjectResult(this AllocationException error)
    {
        var statusCode = error switch
        {
            AllocationNotFoundException => StatusCodes.Status404NotFound,
            AllocationMoveViolatesHardRuleException => StatusCodes.Status409Conflict,
            AllocationInvalidMoveException => StatusCodes.Status400BadRequest,
            AllocationArgumentException => StatusCodes.Status400BadRequest,
            AllocationUnknownException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

        return ErrorResults.Problem(
            statusCode,
            error.Code,
            error.Message);
    }
}