using Application.Rules.Exceptions;
using Domain.Rules;
using LanguageExt;
using MediatR;
using Rule = Domain.Rules.Rule;

namespace Application.Rules.Commands;

public record DeleteRuleCommand : IRequest<Either<RuleException, Rule>>
{
    public Guid SessionId { get; init; }

    public Guid Id { get; init; }
}

public class DeleteRuleCommandHandler
    : IRequestHandler<DeleteRuleCommand, Either<RuleException, Rule>>
{
    public Task<Either<RuleException, Rule>> Handle(
        DeleteRuleCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}