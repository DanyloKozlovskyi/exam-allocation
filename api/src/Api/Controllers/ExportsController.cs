using Api.Constants;
using Application.Allocations.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class ExportsController : ControllerBase
{
    [HttpGet(ApiRoutes.Exports.Export)]
    public Task<ActionResult> Export(
        [FromRoute] Guid allocationId,
        [FromQuery] ExportFormat format,
        CancellationToken cancellationToken,
        [FromQuery] Guid? roomId = null)
    {
        return Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }
}