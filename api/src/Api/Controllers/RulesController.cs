using Api.Constants;
using Api.Dtos;
using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Rules.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class RulesController(ISender sender, IRulesControllerService controllerService) : ControllerBase
{
    [HttpGet(ApiRoutes.Rules.List)]
    public async Task<ActionResult<IReadOnlyList<RuleDto>>> GetAll(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var rules = await controllerService.GetAll(sessionId, cancellationToken);
        return Ok(rules);
    }

    [HttpGet(ApiRoutes.Rules.ById)]
    public async Task<ActionResult<RuleDto>> Get(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid ruleId,
        CancellationToken cancellationToken)
    {
        var rule = await controllerService.Get(sessionId, ruleId, cancellationToken);
        return rule.Match<ActionResult<RuleDto>>(r => r, () => NotFound());
    }

    [HttpGet(ApiRoutes.Rules.RuleTypes)]
    public ActionResult<IReadOnlyList<RuleTypeInfoDto>> RuleTypes()
    {
        return Ok(controllerService.RuleTypes());
    }

    [HttpPost(ApiRoutes.Rules.Create)]
    public async Task<ActionResult<RuleDto>> Create(
        [FromRoute] Guid sessionId,
        [FromBody] RuleDto rule,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(rule.ToCreateCommand(sessionId), cancellationToken);
        return result.Match<ActionResult<RuleDto>>(r => RuleDto.FromDomain(r), e => e.ToObjectResult());
    }

    [HttpPut(ApiRoutes.Rules.Edit)]
    public async Task<ActionResult<RuleDto>> Edit(
        [FromRoute] Guid sessionId,
        [FromBody] RuleDto rule,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(rule.ToUpdateCommand(sessionId), cancellationToken);
        return result.Match<ActionResult<RuleDto>>(r => RuleDto.FromDomain(r), e => e.ToObjectResult());
    }

    [HttpDelete(ApiRoutes.Rules.ById)]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid ruleId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteRuleCommand { SessionId = sessionId, Id = ruleId }, cancellationToken);
        return result.Match<ActionResult>(_ => NoContent(), e => e.ToObjectResult());
    }
}