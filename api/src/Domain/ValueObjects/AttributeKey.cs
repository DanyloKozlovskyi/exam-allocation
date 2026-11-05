using System.Text;
using Domain.Common;

namespace Domain.ValueObjects;

/// <summary>Normalized (lower-case slug) key of a participant attribute.</summary>
public sealed class AttributeKey : ValueObject
{
    public const int MaxLength = 64;
    private const char Separator = '_';

    private AttributeKey(string value)
    {
        Value = value;
    }

    public static AttributeKey Group => new(AttributeKeys.Group);

    public static AttributeKey School => new(AttributeKeys.School);

    public string Value { get; }

    public static AttributeKey From(string raw)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(raw, nameof(raw));
        var slug = Normalize(raw);
        Guard.NotNullOrEmptyOrWhiteSpace(slug, nameof(raw));
        Guard.MaxLength(slug, MaxLength, nameof(raw));
        return new AttributeKey(slug);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static string Normalize(string raw)
    {
        var builder = new StringBuilder(raw.Length);
        foreach (var ch in raw.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (builder.Length > 0 && builder[^1] != Separator)
            {
                builder.Append(Separator);
            }
        }

        return builder.ToString().Trim(Separator);
    }
}