using Domain.Rooms;
using FluentValidation;

namespace Application.Rooms.Commands;

public class UpdateRoomCommandValidator : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomCommandValidator()
    {
        RuleFor(x => x.CenterId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(RoomConstraints.NameMaxLength);
        RuleFor(x => x.Layout).NotNull().SetValidator(new RoomLayoutInputValidator()!);
        RuleFor(x => x.CostOverride).GreaterThanOrEqualTo(RoomConstraints.MinCost);
    }
}