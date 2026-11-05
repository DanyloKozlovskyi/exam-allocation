using Api.Constants;
using Api.Dtos;
using Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class AllocationsController : ControllerBase
{
    [HttpGet(ApiRoutes.Allocations.ByRun)]
    public Task<ActionResult<IReadOnlyList<AllocationSummaryDto>>> GetByRun(
        [FromRoute] Guid runId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<IReadOnlyList<AllocationSummaryDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet(ApiRoutes.Allocations.ById)]
    public Task<ActionResult<AllocationDto>> Get(
        [FromRoute] Guid allocationId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<AllocationDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet(ApiRoutes.Allocations.RoomSeating)]
    public Task<ActionResult<RoomSeatingDto>> RoomSeating(
        [FromRoute] Guid allocationId,
        [FromRoute] Guid roomId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<RoomSeatingDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet(ApiRoutes.Allocations.Participants)]
    public Task<ActionResult<PagedDto<AllocationRowDto>>> Participants(
        [FromRoute] Guid allocationId,
        CancellationToken cancellationToken,
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null)
    {
        return Task.FromResult<ActionResult<PagedDto<AllocationRowDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Allocations.ValidateMoves)]
    public Task<ActionResult<ValidationReportDto>> ValidateMoves(
        [FromRoute] Guid allocationId,
        [FromBody] MovesDto moves,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<ValidationReportDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Allocations.ApplyMoves)]
    public Task<ActionResult<AllocationDto>> ApplyMoves(
        [FromRoute] Guid allocationId,
        [FromBody] MovesDto moves,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<AllocationDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }
}