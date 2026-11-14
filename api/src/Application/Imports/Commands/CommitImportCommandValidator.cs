using FluentValidation;

namespace Application.Imports.Commands;

public class CommitImportCommandValidator : AbstractValidator<CommitImportCommand>
{
    public CommitImportCommandValidator()
    {
        RuleFor(x => x.ImportId).NotEmpty();
        RuleFor(x => x.Options).NotEmpty().SetValidator(new ImportOptionsValidator()!);
    }
}