using Domain.ValueObjects;
using FluentValidation;

namespace Application.Pins.Commands;

public class UpsertPinCommandValidator : AbstractValidator<UpsertPinCommand>
{
    public UpsertPinCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ParticipantId).NotEmpty();
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.SeatKey)
            .Must(x => string.IsNullOrWhiteSpace(x) || SeatKey.TryParse(x.Trim(), out _))
            .WithMessage("'{PropertyValue}' is not a valid seat key");
    }
}