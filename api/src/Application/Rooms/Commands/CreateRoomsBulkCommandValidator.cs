using Domain.Rooms;
using FluentValidation;

namespace Application.Rooms.Commands;

public class CreateRoomsBulkCommandValidator : AbstractValidator<CreateRoomsBulkCommand>
{
    public CreateRoomsBulkCommandValidator()
    {
        RuleFor(x => x.CenterId).NotEmpty();
        RuleFor(x => x.Names)
            .NotEmpty()
            .Must(x => x.Count <= RoomConstraints.MaxBulkRooms)
            .WithMessage($"At most {RoomConstraints.MaxBulkRooms} rooms can be created at once")
            .Must(x => x.Select(n => n?.Trim()).Distinct(StringComparer.Ordinal).Count() == x.Count)
            .WithMessage("Room names must be unique");
        RuleForEach(x => x.Names).NotEmpty().MaximumLength(RoomConstraints.NameMaxLength);
        RuleFor(x => x.Layout).NotNull().SetValidator(new RoomLayoutInputValidator()!);
        RuleFor(x => x.CostOverride).GreaterThanOrEqualTo(RoomConstraints.MinCost);
    }
}