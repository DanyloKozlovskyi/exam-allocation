using Domain.ValueObjects;

namespace Application.Participants.Services;

/// <summary>Normalizes custom participant attributes coming from commands.</summary>
public static class ParticipantAttributes
{
    /// <summary>True when <paramref name="raw"/> can be normalized to a custom (non built-in) attribute key.</summary>
    public static bool IsValidCustomKey(string? raw)
    {
        if (!TryNormalize(raw, out var key))
        {
            return false;
        }

        return key!.Value is not AttributeKeys.Group and not AttributeKeys.School;
    }

    /// <summary>True when no two keys normalize to the same attribute key.</summary>
    public static bool HaveDistinctKeys(IReadOnlyDictionary<string, string>? attributes)
    {
        if (attributes is null)
        {
            return true;
        }

        var normalized = attributes.Keys
            .Select(x => TryNormalize(x, out var key) ? key!.Value : x)
            .ToList();
        return normalized.Distinct(StringComparer.Ordinal).Count() == normalized.Count;
    }

    /// <summary>Converts raw attributes to normalized keys, dropping empty values.</summary>
    public static IReadOnlyDictionary<AttributeKey, string> ToDomain(IReadOnlyDictionary<string, string>? attributes)
    {
        if (attributes is null)
        {
            return new Dictionary<AttributeKey, string>();
        }

        return attributes
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToDictionary(x => AttributeKey.From(x.Key), x => x.Value.Trim());
    }

    private static bool TryNormalize(string? raw, out AttributeKey? key)
    {
        key = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        try
        {
            key = AttributeKey.From(raw);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}