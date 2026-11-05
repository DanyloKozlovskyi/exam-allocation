using FluentValidation;

namespace Application.Rooms.Commands;

public class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.CenterId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Layout).NotEmpty();
    }
}