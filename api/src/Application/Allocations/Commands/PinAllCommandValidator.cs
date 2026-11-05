using FluentValidation;

namespace Application.Allocations.Commands;

public class PinAllCommandValidator : AbstractValidator<PinAllCommand>
{
    public PinAllCommandValidator()
    {
        RuleFor(x => x.AllocationId).NotEmpty();
    }
}