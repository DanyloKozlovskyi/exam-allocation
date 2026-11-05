using Application.Common.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class ErrorResults
{
    public static ObjectResult Problem(int statusCode, ErrorCode code, string message, object? data = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = message,
        };
        problem.Extensions[ErrorExtensionKeys.Code] = code;
        if (data is not null)
        {
            problem.Extensions[ErrorExtensionKeys.Data] = data;
        }

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}