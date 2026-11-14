using System.Globalization;

namespace Tools.Shared.CommandLine;

/// <summary>Minimal <c>--name value</c> / <c>--flag</c> parser shared by the tools.</summary>
public sealed class CommandLineArguments
{
    public const string Prefix = "--";

    private readonly Dictionary<string, string?> values;

    private CommandLineArguments(Dictionary<string, string?> values) => this.values = values;

    public static CommandLineArguments Parse(IReadOnlyList<string> args)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith(Prefix, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected argument '{arg}'");
            }

            var name = arg[Prefix.Length..];
            var hasValue = i + 1 < args.Count && !args[i + 1].StartsWith(Prefix, StringComparison.Ordinal);
            values[name] = hasValue ? args[++i] : null;
        }

        return new CommandLineArguments(values);
    }

    public bool Has(string name) => values.ContainsKey(name);

    public string? GetString(string name) => values.GetValueOrDefault(name);

    public int? GetInt(string name)
        => GetString(name) is { } text ? int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture) : null;

    public double? GetDouble(string name)
        => GetString(name) is { } text ? double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture) : null;

    public TEnum? GetEnum<TEnum>(string name)
        where TEnum : struct, Enum
    {
        if (GetString(name) is not { } text)
        {
            return null;
        }

        return Enum.TryParse<TEnum>(text, ignoreCase: true, out var value) && Enum.IsDefined(value)
            ? value
            : throw new ArgumentException($"Invalid value '{text}' for {Prefix}{name}; expected one of {string.Join('|', Enum.GetNames<TEnum>())}");
    }

    /// <summary>Parses an inclusive range written as <c>min..max</c> (or a single number).</summary>
    public (int Min, int Max)? GetRange(string name, string separator)
    {
        if (GetString(name) is not { } text)
        {
            return null;
        }

        var parts = text.Split(separator, StringSplitOptions.TrimEntries);
        var min = int.Parse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture);
        var max = parts.Length > 1 ? int.Parse(parts[^1], NumberStyles.Integer, CultureInfo.InvariantCulture) : min;
        return min <= max ? (min, max) : throw new ArgumentException($"Invalid range '{text}' for {Prefix}{name}");
    }
}