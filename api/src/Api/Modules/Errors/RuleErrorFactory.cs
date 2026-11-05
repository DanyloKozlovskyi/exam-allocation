using Application.Rules.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class RuleErrorFactory
{
    public static ObjectResult ToObjectResult(this RuleException error)
    {
        var statusCode = error switch
        {
            RuleSessionNotFoundException => StatusCodes.Status404NotFound,
            RuleNotFoundException => StatusCodes.Status404NotFound,
            RuleParametersInvalidException => StatusCodes.Status400BadRequest,
            RuleConcurrencyException => StatusCodes.Status409Conflict,
            RuleArgumentException => StatusCodes.Status400BadRequest,
            RuleUnknownException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

        return ErrorResults.Problem(
            statusCode,
            error.Code,
            error.Message);
    }
}