using FluentValidation;

namespace Application.Rooms.Commands;

public class CreateRoomsBulkCommandValidator : AbstractValidator<CreateRoomsBulkCommand>
{
    public CreateRoomsBulkCommandValidator()
    {
        RuleFor(x => x.CenterId).NotEmpty();
        RuleFor(x => x.Names).NotEmpty();
        RuleFor(x => x.Layout).NotEmpty();
    }
}