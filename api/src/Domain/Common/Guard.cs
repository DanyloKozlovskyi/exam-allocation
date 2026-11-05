namespace Domain.Common;

public static class Guard
{
    public static void NotNull<T>([ValidatedNotNull] T? value, string name, string? message = null)
        where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(name, message ?? "Null argument received");
        }
    }

    public static void NotNullOrEmptyOrWhiteSpace([ValidatedNotNull] string? value, string name, string? message = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message ?? "Empty or whitespace argument received", name);
        }
    }

    public static void MaxLength(string? value, int maxLength, string name, string? message = null)
    {
        if (value is not null && value.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(name, message ?? $"Value length must be less than or equal to {maxLength}");
        }
    }

    public static void InRange<T>(T value, T min, T max, string name, string? message = null)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            throw new ArgumentOutOfRangeException(name, message ?? $"Value must be between {min} and {max}");
        }
    }

    public static void GreaterThanOrEqual<T>(T value, T expected, string name, string? message = null)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(expected) < 0)
        {
            throw new ArgumentOutOfRangeException(name, message ?? $"Value must be greater than or equal to {expected}");
        }
    }

    public static void IsTrue(bool condition, string name, string? message = null)
    {
        if (!condition)
        {
            throw new ArgumentException(message ?? "Condition must be true", name);
        }
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ValidatedNotNullAttribute : Attribute
{
}