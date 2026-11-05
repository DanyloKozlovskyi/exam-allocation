using FluentValidation;

namespace Application.Imports.Commands;

public class UploadImportCommandValidator : AbstractValidator<UploadImportCommand>
{
    public UploadImportCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty();
    }
}