using Application.Rules.Validators;
using FluentValidation;

namespace Application.Rules.Commands;

public class UpdateRuleCommandValidator : AbstractValidator<UpdateRuleCommand>
{
    public UpdateRuleCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        this.AddRuleRules(x => x.Parameters, x => x.IsHard, x => x.Weight, x => x.DisplayName);
    }
}