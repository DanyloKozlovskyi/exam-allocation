using System.Diagnostics.CodeAnalysis;
using Solver.Model;

namespace Solver.Allocation;

/// <summary>Single place deciding whether a participant has a value for a rule-referenced attribute.</summary>
internal static class AttributeValues
{
    public static bool TryGet(SolverParticipant participant, string key, [NotNullWhen(true)] out string? value)
    {
        if (participant.Attributes.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            value = raw;
            return true;
        }

        value = null;
        return false;
    }

    public static bool TryGet(ParticipantClass participantClass, string key, [NotNullWhen(true)] out string? value)
        => participantClass.Values.TryGetValue(key, out value);

    /// <summary>Attribute keys referenced by R2, R3 and R6 rules, ordinal-sorted and distinct.</summary>
    public static IReadOnlyList<string> ReferencedKeys(IEnumerable<SolverRule> rules)
        => rules
            .Select(KeyOf)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

    public static string? KeyOf(SolverRule rule) => rule.Parameters switch
    {
        SeparateByAttributeParameters p => p.AttributeKey,
        NoAdjacentByAttributeParameters p => p.AttributeKey,
        KeepTogetherInCenterParameters p => p.AttributeKey,
        _ => null,
    };
}