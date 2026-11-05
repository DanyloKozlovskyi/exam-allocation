using FluentValidation;

namespace Application.Centers.Commands;

public class CreateTestCenterCommandValidator : AbstractValidator<CreateTestCenterCommand>
{
    public CreateTestCenterCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}