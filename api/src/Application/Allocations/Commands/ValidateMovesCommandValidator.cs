using FluentValidation;

namespace Application.Allocations.Commands;

public class ValidateMovesCommandValidator : AbstractValidator<ValidateMovesCommand>
{
    public ValidateMovesCommandValidator()
    {
        RuleFor(x => x.AllocationId).NotEmpty();
    }
}