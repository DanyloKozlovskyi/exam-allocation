using Application.Rules.Validators;
using FluentValidation;

namespace Application.Rules.Commands;

public class CreateRuleCommandValidator : AbstractValidator<CreateRuleCommand>
{
    public CreateRuleCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        this.AddRuleRules(x => x.Parameters, x => x.IsHard, x => x.Weight, x => x.DisplayName);
    }
}