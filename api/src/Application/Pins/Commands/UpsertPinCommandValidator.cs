using FluentValidation;

namespace Application.Pins.Commands;

public class UpsertPinCommandValidator : AbstractValidator<UpsertPinCommand>
{
    public UpsertPinCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ParticipantId).NotEmpty();
        RuleFor(x => x.RoomId).NotEmpty();
    }
}