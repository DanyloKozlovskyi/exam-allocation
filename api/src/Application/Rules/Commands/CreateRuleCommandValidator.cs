using FluentValidation;

namespace Application.Rules.Commands;

public class CreateRuleCommandValidator : AbstractValidator<CreateRuleCommand>
{
    public CreateRuleCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Parameters).NotEmpty();
    }
}