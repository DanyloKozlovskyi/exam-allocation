using Application.Participants.Services;
using Domain.Participants;
using Domain.ValueObjects;
using FluentValidation;

namespace Application.Participants.Commands;

/// <summary>Shared FluentValidation rules for participant create/update commands.</summary>
public static class ParticipantCommandRules
{
    public static void AddParticipantRules<T>(
        this AbstractValidator<T> validator,
        Func<T, string?> externalId,
        Func<T, string?> lastName,
        Func<T, string?> firstName,
        Func<T, string?> middleName,
        Func<T, string?> group,
        Func<T, string?> school,
        Func<T, IReadOnlyDictionary<string, string>> attributes,
        Func<T, string?> notes)
    {
        validator.RuleFor(x => externalId(x)).MaximumLength(ParticipantConstraints.ExternalIdMaxLength).OverridePropertyName(nameof(Participant.ExternalId));
        validator.RuleFor(x => lastName(x)).NotEmpty().MaximumLength(PersonName.PartMaxLength).OverridePropertyName(nameof(PersonName.LastName));
        validator.RuleFor(x => firstName(x)).NotEmpty().MaximumLength(PersonName.PartMaxLength).OverridePropertyName(nameof(PersonName.FirstName));
        validator.RuleFor(x => middleName(x)).MaximumLength(PersonName.PartMaxLength).OverridePropertyName(nameof(PersonName.MiddleName));
        validator.RuleFor(x => group(x)).MaximumLength(ParticipantConstraints.GroupMaxLength).OverridePropertyName(nameof(Participant.Group));
        validator.RuleFor(x => school(x)).MaximumLength(ParticipantConstraints.SchoolMaxLength).OverridePropertyName(nameof(Participant.School));
        validator.RuleFor(x => notes(x)).MaximumLength(ParticipantConstraints.NotesMaxLength).OverridePropertyName(nameof(Participant.Notes));
        validator.RuleFor(x => attributes(x))
            .NotNull()
            .Must(x => x is null || x.Count <= ParticipantConstraints.MaxAttributes)
            .WithMessage($"At most {ParticipantConstraints.MaxAttributes} attributes are allowed")
            .Must(x => x is null || x.Keys.All(ParticipantAttributes.IsValidCustomKey))
            .WithMessage("Attribute keys must be non-empty and must not reuse built-in keys")
            .Must(ParticipantAttributes.HaveDistinctKeys)
            .WithMessage("Attribute keys must be unique after normalization")
            .Must(x => x is null || x.Values.All(v => v is null || v.Length <= ParticipantConstraints.AttributeValueMaxLength))
            .WithMessage($"Attribute values must be at most {ParticipantConstraints.AttributeValueMaxLength} characters")
            .OverridePropertyName(nameof(Participant.Attributes));
    }
}