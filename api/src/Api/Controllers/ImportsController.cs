using Api.Constants;
using Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class ImportsController : ControllerBase
{
    [HttpPost(ApiRoutes.Imports.Upload)]
    public Task<ActionResult<ImportPreviewDto>> Upload(
        [FromRoute] Guid sessionId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<ImportPreviewDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Imports.Validate)]
    public Task<ActionResult<ImportValidationDto>> Validate(
        [FromRoute] Guid importId,
        [FromBody] ImportMappingDto mapping,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<ImportValidationDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Imports.Commit)]
    public Task<ActionResult<ImportResultDto>> Commit(
        [FromRoute] Guid importId,
        [FromBody] ImportMappingDto mapping,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<ImportResultDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet(ApiRoutes.Imports.Template)]
    public Task<ActionResult> Template(
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }
}