using FluentValidation;

namespace Application.Pins.Commands;

public class DeleteAllPinsCommandValidator : AbstractValidator<DeleteAllPinsCommand>
{
    public DeleteAllPinsCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}