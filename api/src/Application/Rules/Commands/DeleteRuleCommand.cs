using Application.Common.Interfaces.Repositories;
using Application.Rules.Exceptions;
using Domain.Rules;
using Domain.Sessions;
using LanguageExt;
using MediatR;
using Rule = Domain.Rules.Rule;

namespace Application.Rules.Commands;

public record DeleteRuleCommand : IRequest<Either<RuleException, Rule>>
{
    public Guid SessionId { get; init; }

    public Guid Id { get; init; }
}

public class DeleteRuleCommandHandler(IRuleRepository ruleRepository)
    : IRequestHandler<DeleteRuleCommand, Either<RuleException, Rule>>
{
    public Task<Either<RuleException, Rule>> Handle(
        DeleteRuleCommand request,
        CancellationToken cancellationToken)
    {
        var id = new RuleId(request.Id);
        var sessionId = new ExamSessionId(request.SessionId);

        return GetRule(sessionId, id, cancellationToken)
            .BindAsync(rule => DeleteEntity(rule, cancellationToken));
    }

    private async Task<Either<RuleException, Rule>> GetRule(ExamSessionId sessionId, RuleId id, CancellationToken cancellationToken)
    {
        var rule = await ruleRepository.GetById(sessionId, id, cancellationToken);
        return rule.Match<Either<RuleException, Rule>>(
            r => r,
            () => new RuleNotFoundException(id));
    }

    private async Task<Either<RuleException, Rule>> DeleteEntity(Rule rule, CancellationToken cancellationToken)
    {
        try
        {
            return await ruleRepository.RemoveAsync(rule, cancellationToken);
        }
        catch (Exception exception)
        {
            return new RuleUnknownException(rule.Id, exception);
        }
    }
}