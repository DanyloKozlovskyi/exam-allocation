using Application.Common.Errors;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Modules;

internal static class ExceptionHandlingModule
{
    internal static void SetupExceptionHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<ValidationExceptionHandler>();
    }
}

/// <summary>Maps FluentValidation failures from ValidationBehaviour to 400 ValidationProblemDetails.</summary>
internal sealed class ValidationExceptionHandler(IOptions<JsonOptions> jsonOptions) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var errors = validationException.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
        };
        problem.Extensions[ErrorExtensionKeys.Code] = ErrorCode.ValidationFailed;

        // Serialize with the MVC JSON options so enums (the error code) are written as strings, like every other error.
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problem, jsonOptions.Value.JsonSerializerOptions, contentType: null, cancellationToken);
        return true;
    }
}