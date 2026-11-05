using FluentValidation;

namespace Application.Sessions.Commands;

public class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}