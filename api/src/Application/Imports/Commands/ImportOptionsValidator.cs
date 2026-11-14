using Application.Imports.Models;
using FluentValidation;

namespace Application.Imports.Commands;

/// <summary>Rules shared by the validate and commit commands.</summary>
public class ImportOptionsValidator : AbstractValidator<ImportOptions>
{
    public ImportOptionsValidator()
    {
        RuleFor(x => x.Sheet).NotEmpty();
        RuleFor(x => x.HeaderRow).GreaterThanOrEqualTo(ImportLimits.DefaultHeaderRow);
        RuleFor(x => x.Mode).IsInEnum();
        RuleFor(x => x.DedupeBy).IsInEnum();
        RuleFor(x => x.Mapping).NotNull();
        RuleFor(x => x.Mapping.Fields)
            .Must(HaveName)
            .WithMessage("Mapping must contain last and first name columns or a full name column")
            .When(x => x.Mapping is not null);
        RuleForEach(x => x.Mapping.Fields.Values)
            .NotEmpty()
            .When(x => x.Mapping is not null);
        RuleForEach(x => x.Mapping.Attributes.Values)
            .NotEmpty()
            .When(x => x.Mapping is not null);
    }

    private static bool HaveName(IReadOnlyDictionary<ParticipantField, string> fields)
        => fields.ContainsKey(ParticipantField.FullName)
           || (fields.ContainsKey(ParticipantField.LastName) && fields.ContainsKey(ParticipantField.FirstName));
}