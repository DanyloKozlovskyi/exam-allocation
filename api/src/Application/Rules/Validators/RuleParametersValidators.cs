using Domain.Rules;
using Domain.Rules.Parameters;
using Domain.ValueObjects;
using FluentValidation;

namespace Application.Rules.Validators;

/// <summary>Shared rule-level validation (parameters per <see cref="RuleType"/>, hard/soft policy, limits).</summary>
public static class RuleValidationRules
{
    public const string AttributeKeyMessage = "'{PropertyValue}' is not a normalized attribute key";
    public const string AdjacencyMessage = "Adjacency kinds must be a non-empty list of distinct, known kinds";

    public static void AddRuleRules<T>(
        this AbstractValidator<T> validator,
        Func<T, RuleParameters?> parameters,
        Func<T, bool> isHard,
        Func<T, int> weight,
        Func<T, string?> displayName)
    {
        validator.RuleFor(x => parameters(x))
            .NotNull()
            .SetInheritanceValidator(v =>
            {
                v.Add(new SeparateByAttributeParametersValidator());
                v.Add(new NoAdjacentByAttributeParametersValidator());
                v.Add(new MinRoomFillParametersValidator());
                v.Add(new KeepTogetherInCenterParametersValidator());
                v.Add(new SpreadInRoomParametersValidator());
            })
            .OverridePropertyName(nameof(Rule.Parameters));
        validator.RuleFor(x => isHard(x))
            .Must((command, hard) => parameters(command) is not { } p || RuleConstraints.Allows(p.RuleType, hard))
            .WithMessage("This rule type does not support the requested hard/soft mode")
            .OverridePropertyName(nameof(Rule.IsHard));
        validator.RuleFor(x => weight(x))
            .InclusiveBetween(RuleConstraints.MinWeight, RuleConstraints.MaxWeight)
            .OverridePropertyName(nameof(Rule.Weight));
        validator.RuleFor(x => displayName(x))
            .MaximumLength(RuleConstraints.DisplayNameMaxLength)
            .OverridePropertyName(nameof(Rule.DisplayName));
    }

    public static bool IsNormalizedAttributeKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        try
        {
            return AttributeKey.From(raw).Value == raw;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool IsValidAdjacency(IReadOnlyList<AdjacencyKind>? kinds)
        => kinds is { Count: > 0 }
           && kinds.All(Enum.IsDefined)
           && kinds.Distinct().Count() == kinds.Count;
}

public class SeparateByAttributeParametersValidator : AbstractValidator<SeparateByAttributeParameters>
{
    public SeparateByAttributeParametersValidator()
    {
        RuleFor(x => x.AttributeKey).Must(RuleValidationRules.IsNormalizedAttributeKey).WithMessage(RuleValidationRules.AttributeKeyMessage);
        RuleFor(x => x.Scope).IsInEnum();
        RuleFor(x => x.MaxPerScope).InclusiveBetween(RuleConstraints.MinMaxPerScope, RuleConstraints.MaxMaxPerScope);
    }
}

public class NoAdjacentByAttributeParametersValidator : AbstractValidator<NoAdjacentByAttributeParameters>
{
    public NoAdjacentByAttributeParametersValidator()
    {
        RuleFor(x => x.AttributeKey).Must(RuleValidationRules.IsNormalizedAttributeKey).WithMessage(RuleValidationRules.AttributeKeyMessage);
        RuleFor(x => x.Adjacency).Must(RuleValidationRules.IsValidAdjacency).WithMessage(RuleValidationRules.AdjacencyMessage);
    }
}

public class MinRoomFillParametersValidator : AbstractValidator<MinRoomFillParameters>
{
    public MinRoomFillParametersValidator()
    {
        RuleFor(x => x.MinFillRatio).GreaterThan(RuleConstraints.MinFillRatio).LessThanOrEqualTo(RuleConstraints.MaxFillRatio);
    }
}

public class KeepTogetherInCenterParametersValidator : AbstractValidator<KeepTogetherInCenterParameters>
{
    public KeepTogetherInCenterParametersValidator()
    {
        RuleFor(x => x.AttributeKey).Must(RuleValidationRules.IsNormalizedAttributeKey).WithMessage(RuleValidationRules.AttributeKeyMessage);
    }
}

public class SpreadInRoomParametersValidator : AbstractValidator<SpreadInRoomParameters>
{
    public SpreadInRoomParametersValidator()
    {
        RuleFor(x => x.Adjacency).Must(RuleValidationRules.IsValidAdjacency).WithMessage(RuleValidationRules.AdjacencyMessage);
    }
}