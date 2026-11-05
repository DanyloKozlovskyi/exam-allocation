using FluentValidation;

namespace Application.Centers.Commands;

public class UpdateTestCenterCommandValidator : AbstractValidator<UpdateTestCenterCommand>
{
    public UpdateTestCenterCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}