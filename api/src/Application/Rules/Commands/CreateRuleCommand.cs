using Application.Rules.Exceptions;
using Domain.Rules;
using Domain.Rules.Parameters;
using LanguageExt;
using MediatR;
using Rule = Domain.Rules.Rule;

namespace Application.Rules.Commands;

public record CreateRuleCommand : IRequest<Either<RuleException, Rule>>
{
    public Guid SessionId { get; init; }

    public RuleParameters? Parameters { get; init; }

    public bool IsHard { get; init; }

    public int Weight { get; init; } = RuleConstraints.DefaultWeight;

    public bool IsEnabled { get; init; } = true;

    public string? DisplayName { get; init; }
}

public class CreateRuleCommandHandler
    : IRequestHandler<CreateRuleCommand, Either<RuleException, Rule>>
{
    public Task<Either<RuleException, Rule>> Handle(
        CreateRuleCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}