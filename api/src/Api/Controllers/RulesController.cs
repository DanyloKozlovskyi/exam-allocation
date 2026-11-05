using Api.Constants;
using Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class RulesController : ControllerBase
{
    [HttpGet(ApiRoutes.Rules.List)]
    public Task<ActionResult<IReadOnlyList<RuleDto>>> GetAll(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<IReadOnlyList<RuleDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet(ApiRoutes.Rules.ById)]
    public Task<ActionResult<RuleDto>> Get(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid ruleId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<RuleDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet(ApiRoutes.Rules.RuleTypes)]
    public Task<ActionResult<IReadOnlyList<RuleTypeInfoDto>>> RuleTypes(
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<IReadOnlyList<RuleTypeInfoDto>>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost(ApiRoutes.Rules.Create)]
    public Task<ActionResult<RuleDto>> Create(
        [FromRoute] Guid sessionId,
        [FromBody] RuleDto rule,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<RuleDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPut(ApiRoutes.Rules.Edit)]
    public Task<ActionResult<RuleDto>> Edit(
        [FromRoute] Guid sessionId,
        [FromBody] RuleDto rule,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<RuleDto>>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpDelete(ApiRoutes.Rules.ById)]
    public Task<ActionResult> Delete(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid ruleId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }
}