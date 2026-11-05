using FluentValidation;

namespace Application.Allocations.Commands;

public class ApplyMovesCommandValidator : AbstractValidator<ApplyMovesCommand>
{
    public ApplyMovesCommandValidator()
    {
        RuleFor(x => x.AllocationId).NotEmpty();
    }
}