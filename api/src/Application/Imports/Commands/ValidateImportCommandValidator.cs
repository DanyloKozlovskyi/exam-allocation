using FluentValidation;

namespace Application.Imports.Commands;

public class ValidateImportCommandValidator : AbstractValidator<ValidateImportCommand>
{
    public ValidateImportCommandValidator()
    {
        RuleFor(x => x.ImportId).NotEmpty();
        RuleFor(x => x.Options).NotEmpty();
    }
}