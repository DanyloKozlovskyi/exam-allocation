using Api.Constants;
using Api.Dtos;
using Application.Common.Models;
using Application.Participants.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class ParticipantsController : ControllerBase
{
    [HttpGet(ApiRoutes.Participants.List)]
    public Task<ActionResult<PagedDto<ParticipantDto>>> GetPage(
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
        return Task.FromResult<ActionResult<PagedDto<ParticipantDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet(ApiRoutes.Participants.ById)]
    public Task<ActionResult<ParticipantDto>> Get(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid participantId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<ParticipantDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Participants.Create)]
    public Task<ActionResult<ParticipantDto>> Create(
        [FromRoute] Guid sessionId,
        [FromBody] ParticipantDto participant,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<ParticipantDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPut(ApiRoutes.Participants.Edit)]
    public Task<ActionResult<ParticipantDto>> Edit(
        [FromRoute] Guid sessionId,
        [FromBody] ParticipantDto participant,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<ParticipantDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpDelete(ApiRoutes.Participants.ById)]
    public Task<ActionResult> Delete(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid participantId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Participants.DeleteAll)]
    public Task<ActionResult> DeleteAll(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet(ApiRoutes.Participants.Attributes)]
    public Task<ActionResult<IReadOnlyList<AttributeInfoDto>>> Attributes(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<IReadOnlyList<AttributeInfoDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }
}