using FluentValidation;

namespace Application.Rules.Commands;

public class UpdateRuleCommandValidator : AbstractValidator<UpdateRuleCommand>
{
    public UpdateRuleCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Parameters).NotEmpty();
    }
}