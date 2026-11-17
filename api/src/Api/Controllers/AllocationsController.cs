using Api.Constants;
using Api.Dtos;
using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Allocations.Commands;
using Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class AllocationsController(ISender sender, IAllocationsControllerService controllerService) : ControllerBase
{
    [HttpGet(ApiRoutes.Allocations.ByRun)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AllocationSummaryDto>>> GetByRun(
        [FromRoute] Guid runId,
        CancellationToken cancellationToken)
    {
        var allocations = await controllerService.GetByRun(runId, cancellationToken);
        return Ok(allocations);
    }

    [HttpGet(ApiRoutes.Allocations.ById)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AllocationDto>> Get(
        [FromRoute] Guid allocationId,
        CancellationToken cancellationToken)
    {
        var allocation = await controllerService.Get(allocationId, cancellationToken);
        return allocation.Match<ActionResult<AllocationDto>>(dto => dto, () => NotFound());
    }

    [HttpGet(ApiRoutes.Allocations.RoomSeating)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomSeatingDto>> RoomSeating(
        [FromRoute] Guid allocationId,
        [FromRoute] Guid roomId,
        CancellationToken cancellationToken)
    {
        var seating = await controllerService.RoomSeating(allocationId, roomId, cancellationToken);
        return seating.Match<ActionResult<RoomSeatingDto>>(dto => dto, () => NotFound());
    }

    [HttpGet(ApiRoutes.Allocations.Participants)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedDto<AllocationRowDto>>> Participants(
        [FromRoute] Guid allocationId,
        CancellationToken cancellationToken,
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] SortDirection sortDirection = SortDirection.Ascending)
    {
        var rows = await controllerService.Participants(allocationId, new PageRequest(page, pageSize, search, sortDirection), cancellationToken);
        return rows.Match<ActionResult<PagedDto<AllocationRowDto>>>(dto => dto, () => NotFound());
    }

    [HttpPost(ApiRoutes.Allocations.ValidateMoves)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ValidationReportDto>> ValidateMoves(
        [FromRoute] Guid allocationId,
        [FromBody] MovesDto moves,
        CancellationToken cancellationToken)
    {
        var command = new ValidateMovesCommand { AllocationId = allocationId, Moves = moves.Moves.Select(x => x.ToModel()).ToList() };
        var result = await sender.Send(command, cancellationToken);
        return result.Match<ActionResult<ValidationReportDto>>(report => ValidationReportDto.FromModel(report), e => e.ToObjectResult());
    }

    [HttpPost(ApiRoutes.Allocations.ApplyMoves)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AllocationDto>> ApplyMoves(
        [FromRoute] Guid allocationId,
        [FromBody] MovesDto moves,
        CancellationToken cancellationToken)
    {
        var command = new ApplyMovesCommand { AllocationId = allocationId, Moves = moves.Moves.Select(x => x.ToModel()).ToList(), Force = moves.Force };
        var result = await sender.Send(command, cancellationToken);
        return await result.MatchAsync<ActionResult<AllocationDto>>(
            async allocation => (await controllerService.Get(allocation.Id.Value, cancellationToken)).Match<ActionResult<AllocationDto>>(dto => dto, () => NotFound()),
            e => e.ToObjectResult());
    }
}