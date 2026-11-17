using Api.Constants;
using Api.Dtos;
using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Common.Models;
using Application.Participants.Commands;
using Application.Participants.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class ParticipantsController(ISender sender, IParticipantsControllerService controllerService) : ControllerBase
{
    [HttpGet(ApiRoutes.Participants.List)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedDto<ParticipantDto>>> GetPage(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken,
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] ParticipantSortField sortBy = ParticipantSortField.LastName,
        [FromQuery] SortDirection sortDirection = SortDirection.Ascending,
        [FromQuery] string? group = null,
        [FromQuery] string? school = null,
        [FromQuery] bool? needsAccessibleSeat = null)
    {
        var filter = new ParticipantFilter(group, school, needsAccessibleSeat, sortBy);
        var request = new PageRequest(page, pageSize, search, sortDirection);
        var result = await controllerService.GetPage(sessionId, filter, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.Participants.ById)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantDto>> Get(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid participantId,
        CancellationToken cancellationToken)
    {
        var participant = await controllerService.Get(sessionId, participantId, cancellationToken);
        return participant.Match<ActionResult<ParticipantDto>>(p => p, () => NotFound());
    }

    [HttpPost(ApiRoutes.Participants.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ParticipantDto>> Create(
        [FromRoute] Guid sessionId,
        [FromBody] ParticipantDto participant,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(participant.ToCreateCommand(sessionId), cancellationToken);
        return result.Match<ActionResult<ParticipantDto>>(p => ParticipantDto.FromDomain(p), e => e.ToObjectResult());
    }

    [HttpPut(ApiRoutes.Participants.Edit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ParticipantDto>> Edit(
        [FromRoute] Guid sessionId,
        [FromBody] ParticipantDto participant,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(participant.ToUpdateCommand(sessionId), cancellationToken);
        return result.Match<ActionResult<ParticipantDto>>(p => ParticipantDto.FromDomain(p), e => e.ToObjectResult());
    }

    [HttpDelete(ApiRoutes.Participants.ById)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid participantId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteParticipantCommand { SessionId = sessionId, Id = participantId }, cancellationToken);
        return result.Match<ActionResult>(_ => NoContent(), e => e.ToObjectResult());
    }

    [HttpPost(ApiRoutes.Participants.DeleteAll)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteAll(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteAllParticipantsCommand { SessionId = sessionId }, cancellationToken);
        return result.Match<ActionResult>(_ => NoContent(), e => e.ToObjectResult());
    }

    [HttpGet(ApiRoutes.Participants.Attributes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AttributeInfoDto>>> Attributes(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var attributes = await controllerService.Attributes(sessionId, cancellationToken);
        return Ok(attributes);
    }
}