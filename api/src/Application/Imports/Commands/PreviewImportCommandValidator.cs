using Application.Imports.Models;
using FluentValidation;

namespace Application.Imports.Commands;

public class PreviewImportCommandValidator : AbstractValidator<PreviewImportCommand>
{
    public PreviewImportCommandValidator()
    {
        RuleFor(x => x.ImportId).NotEmpty();
        RuleFor(x => x.Sheet).NotEmpty();
        RuleFor(x => x.HeaderRow).GreaterThanOrEqualTo(ImportLimits.DefaultHeaderRow);
    }
}