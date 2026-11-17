using Api.Constants;
using Api.Dtos;
using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Imports.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class ImportsController(ISender sender, IImportsControllerService controllerService) : ControllerBase
{
    [HttpPost(ApiRoutes.Imports.Upload)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ImportPreviewDto>> Upload(
        [FromRoute] Guid sessionId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        var command = new UploadImportCommand
        {
            SessionId = sessionId,
            FileName = file.FileName,
            Content = buffer.ToArray(),
        };

        var result = await sender.Send(command, cancellationToken);
        return result.Match<ActionResult<ImportPreviewDto>>(p => ImportPreviewDto.FromModel(p), e => e.ToObjectResult());
    }

    [HttpPost(ApiRoutes.Imports.Preview)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ImportPreviewDto>> Preview(
        [FromRoute] Guid importId,
        [FromBody] ImportPreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(importId), cancellationToken);
        return result.Match<ActionResult<ImportPreviewDto>>(p => ImportPreviewDto.FromModel(p), e => e.ToObjectResult());
    }

    [HttpPost(ApiRoutes.Imports.Validate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ImportValidationDto>> Validate(
        [FromRoute] Guid importId,
        [FromBody] ImportMappingDto mapping,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(mapping.ToValidateCommand(importId), cancellationToken);
        return result.Match<ActionResult<ImportValidationDto>>(v => ImportValidationDto.FromModel(v), e => e.ToObjectResult());
    }

    [HttpPost(ApiRoutes.Imports.Commit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ImportResultDto>> Commit(
        [FromRoute] Guid importId,
        [FromBody] ImportMappingDto mapping,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(mapping.ToCommitCommand(importId), cancellationToken);
        return result.Match<ActionResult<ImportResultDto>>(r => ImportResultDto.FromModel(r), e => e.ToObjectResult());
    }

    [HttpGet(ApiRoutes.Imports.Template)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult Template()
    {
        var template = controllerService.Template();
        return File(template.Content, template.ContentType, template.FileName);
    }
}