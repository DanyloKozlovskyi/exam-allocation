using Application.Imports.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class ImportErrorFactory
{
    public static ObjectResult ToObjectResult(this ImportException error)
    {
        var statusCode = error switch
        {
            ImportSessionNotFoundException => StatusCodes.Status404NotFound,
            ImportNotFoundException => StatusCodes.Status404NotFound,
            ImportFileInvalidException => StatusCodes.Status400BadRequest,
            ImportFileTooLargeException => StatusCodes.Status413PayloadTooLarge,
            ImportRowsInvalidException => StatusCodes.Status422UnprocessableEntity,
            ImportArgumentException => StatusCodes.Status400BadRequest,
            ImportUnknownException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

        return ErrorResults.Problem(
            statusCode,
            error.Code,
            error.Message);
    }
}