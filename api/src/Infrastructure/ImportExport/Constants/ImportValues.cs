namespace Infrastructure.ImportExport.Constants;

/// <summary>Cell values and characters recognised while parsing import files.</summary>
public static class ImportValues
{
    /// <summary>Canonical apostrophe used after normalization.</summary>
    public const char Apostrophe = '\'';

    public const char Space = ' ';

    /// <summary>Apostrophe variants normalized to <see cref="Apostrophe"/> (typographic, modifier letter, grave, acute).</summary>
    public static IReadOnlyList<char> ApostropheVariants { get; } = ['\u2019', '\u02BC', '\u0060', '\u00B4', '\u2018'];

    public static IReadOnlySet<string> TrueValues { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "так", "yes", "1", "+", "true", "да", "y", "т",
    };

    public static IReadOnlySet<string> FalseValues { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ні", "no", "0", "-", "false", "нет", "n", "н",
    };
}