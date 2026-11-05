using FluentValidation;

namespace Application.Pins.Commands;

public class DeletePinCommandValidator : AbstractValidator<DeletePinCommand>
{
    public DeletePinCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ParticipantId).NotEmpty();
    }
}