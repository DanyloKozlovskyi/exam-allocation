using FluentValidation;

namespace Application.Rules.Commands;

public class DeleteRuleCommandValidator : AbstractValidator<DeleteRuleCommand>
{
    public DeleteRuleCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}