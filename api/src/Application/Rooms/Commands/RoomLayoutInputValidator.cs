using Application.Rooms.Models;
using Domain.Rooms;
using Domain.ValueObjects;
using FluentValidation;

namespace Application.Rooms.Commands;

/// <summary>Child validator for room layouts (used by room commands and the layout preview).</summary>
public class RoomLayoutInputValidator : AbstractValidator<RoomLayoutInput>
{
    public RoomLayoutInputValidator()
    {
        RuleFor(x => x.Rows).InclusiveBetween(RoomLayout.MinSize, RoomLayout.MaxRows);
        RuleFor(x => x.Columns).InclusiveBetween(RoomLayout.MinSize, RoomLayout.MaxColumns);
        RuleFor(x => x.SeatsPerDesk).InclusiveBetween(RoomLayout.MinSeatsPerDesk, RoomLayout.MaxSeatsPerDesk);
        RuleFor(x => x.AislesAfterColumns).NotNull();
        RuleFor(x => x.DisabledSeats).NotNull();
        RuleFor(x => x.AccessibleSeats).NotNull();
        RuleForEach(x => x.AislesAfterColumns)
            .Must((layout, aisle) => aisle >= 0 && aisle < layout.Columns - 1)
            .WithMessage("Aisle must be between two existing columns");
        RuleForEach(x => x.DisabledSeats)
            .Must((layout, key) => IsSeatInside(layout, key))
            .WithMessage("'{PropertyValue}' is not a seat key of this layout");
        RuleForEach(x => x.AccessibleSeats)
            .Must((layout, key) => IsSeatInside(layout, key))
            .WithMessage("'{PropertyValue}' is not a seat key of this layout");
    }

    private static bool IsSeatInside(RoomLayoutInput layout, string? key)
        => SeatKey.TryParse(key, out var seatKey)
           && seatKey!.Row < layout.Rows
           && seatKey.Column < layout.Columns
           && seatKey.Seat < layout.SeatsPerDesk;
}