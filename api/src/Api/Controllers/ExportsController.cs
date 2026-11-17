using Api.Constants;
using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Allocations.Models;
using Application.Common.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class ExportsController(IExportsControllerService controllerService) : ControllerBase
{
    [HttpGet(ApiRoutes.Exports.Export)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Export(
        [FromRoute] Guid allocationId,
        [FromQuery] ExportFormat format,
        CancellationToken cancellationToken,
        [FromQuery] Guid? roomId = null)
    {
        var model = await controllerService.GetModel(allocationId, cancellationToken);
        return await model.Match(
            m => Export(m, format, roomId, cancellationToken),
            () => Task.FromResult<ActionResult>(ErrorResults.Problem(
                StatusCodes.Status404NotFound,
                ErrorCode.AllocationNotFound,
                $"Allocation with id {allocationId} does not exist")));
    }

    private async Task<ActionResult> Export(AllocationExportModel model, ExportFormat format, Guid? roomId, CancellationToken cancellationToken)
    {
        if (roomId is { } id && !model.Centers.Any(c => c.Rooms.Any(r => r.Id == id)))
        {
            return ErrorResults.Problem(StatusCodes.Status404NotFound, ErrorCode.RoomNotFound, $"Room with id {id} is not part of the allocation");
        }

        var file = await controllerService.Export(model, format, roomId, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}