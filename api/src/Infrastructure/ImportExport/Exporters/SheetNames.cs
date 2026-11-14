using System.Globalization;
using System.Text;
using Infrastructure.ImportExport.Constants;

namespace Infrastructure.ImportExport.Exporters;

/// <summary>Builds unique, Excel-valid sheet names (≤ 31 chars, case-insensitive uniqueness).</summary>
public sealed class SheetNames
{
    private readonly HashSet<string> used = new(StringComparer.OrdinalIgnoreCase);

    public SheetNames(IEnumerable<string> reserved)
    {
        foreach (var name in reserved)
        {
            used.Add(name);
        }
    }

    public string ForRoom(string centerName, string roomName)
    {
        var center = Sanitize(centerName);
        if (center.Length > ExportSheets.CenterShortLength)
        {
            center = center[..ExportSheets.CenterShortLength].TrimEnd();
        }

        var name = Sanitize($"{center}{ExportSheets.RoomSheetSeparator}{Sanitize(roomName)}");
        return Unique(name.Length == 0 ? ExportSheets.Fallback : name);
    }

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim())
        {
            if (!ExportSheets.InvalidNameChars.Contains(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Trim().Trim(ImportValues.Apostrophe);
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength].TrimEnd();

    private string Unique(string name)
    {
        var candidate = Truncate(name, ExportSheets.MaxSheetNameLength);
        for (var number = ExportSheets.FirstDuplicateNumber; !used.Add(candidate); number++)
        {
            var suffix = string.Format(CultureInfo.InvariantCulture, ExportSheets.DuplicateSuffixFormat, number);
            candidate = Truncate(name, ExportSheets.MaxSheetNameLength - suffix.Length) + suffix;
        }

        return candidate;
    }
}