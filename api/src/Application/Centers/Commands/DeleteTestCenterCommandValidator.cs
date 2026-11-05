using FluentValidation;

namespace Application.Centers.Commands;

public class DeleteTestCenterCommandValidator : AbstractValidator<DeleteTestCenterCommand>
{
    public DeleteTestCenterCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}