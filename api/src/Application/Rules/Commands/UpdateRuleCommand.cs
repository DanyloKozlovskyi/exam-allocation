using Application.Common.Interfaces.Repositories;
using Application.Rules.Exceptions;
using Domain.Rules;
using Domain.Rules.Parameters;
using Domain.Sessions;
using LanguageExt;
using MediatR;
using Rule = Domain.Rules.Rule;

namespace Application.Rules.Commands;

public record UpdateRuleCommand : IRequest<Either<RuleException, Rule>>
{
    public Guid SessionId { get; init; }

    public Guid Id { get; init; }

    public RuleParameters? Parameters { get; init; }

    public bool IsHard { get; init; }

    public int Weight { get; init; } = RuleConstraints.DefaultWeight;

    public bool IsEnabled { get; init; } = true;

    public string? DisplayName { get; init; }

    public uint Version { get; init; }
}

public class UpdateRuleCommandHandler(IRuleRepository ruleRepository)
    : IRequestHandler<UpdateRuleCommand, Either<RuleException, Rule>>
{
    public Task<Either<RuleException, Rule>> Handle(
        UpdateRuleCommand request,
        CancellationToken cancellationToken)
    {
        var id = new RuleId(request.Id);
        var sessionId = new ExamSessionId(request.SessionId);

        return GetRule(sessionId, id, request.Version, cancellationToken)
            .BindAsync(rule => UpdateEntity(rule, request, cancellationToken));
    }

    private async Task<Either<RuleException, Rule>> GetRule(ExamSessionId sessionId, RuleId id, uint version, CancellationToken cancellationToken)
    {
        var rule = await ruleRepository.GetById(sessionId, id, cancellationToken);
        return rule.Match<Either<RuleException, Rule>>(
            r => r.Version == version ? r : new RuleConcurrencyException(id),
            () => new RuleNotFoundException(id));
    }

    private async Task<Either<RuleException, Rule>> UpdateEntity(Rule rule, UpdateRuleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            rule.UpdateDetails(request.Parameters!, request.IsHard, request.Weight, request.IsEnabled, request.DisplayName);
            return await ruleRepository.UpdateAsync(rule, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return new RuleParametersInvalidException(rule.Id, exception.Message);
        }
        catch (Exception exception)
        {
            return new RuleUnknownException(rule.Id, exception);
        }
    }
}