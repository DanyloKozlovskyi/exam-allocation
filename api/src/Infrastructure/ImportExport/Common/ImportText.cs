using System.Text;
using Infrastructure.ImportExport.Constants;

namespace Infrastructure.ImportExport.Common;

/// <summary>Whitespace and apostrophe normalization shared by the suggester and the row validator.</summary>
public static class ImportText
{
    /// <summary>Trims and collapses runs of whitespace into one space; returns <c>null</c> for blank input.</summary>
    public static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var ch in value.Trim())
        {
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(ImportValues.Space);
                pendingSpace = false;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    /// <summary>Key used to compare headers: cleaned, lower-case, apostrophes unified.</summary>
    public static string HeaderKey(string? header)
    {
        var cleaned = Clean(header);
        if (cleaned is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(cleaned.Length);
        foreach (var ch in cleaned.ToLowerInvariant())
        {
            builder.Append(ImportValues.ApostropheVariants.Contains(ch) ? ImportValues.Apostrophe : ch);
        }

        return builder.ToString();
    }
}