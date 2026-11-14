namespace Infrastructure.ImportExport.Constants;

public static class ExportSheets
{
    public const string Summary = "Зведення";
    public const string AllParticipants = "Усі учасники";
    public const string Template = "Учасники";
    public const int MaxSheetNameLength = 31;

    /// <summary>Maximum length of the center part in a room sheet name.</summary>
    public const int CenterShortLength = 12;
    public const string RoomSheetSeparator = "–";
    public const string DuplicateSuffixFormat = " ({0})";
    public const int FirstDuplicateNumber = 2;
    public const string Fallback = "Аудиторія";

    /// <summary>Characters Excel does not allow in sheet names.</summary>
    public static IReadOnlyList<char> InvalidNameChars { get; } = [':', '\\', '/', '?', '*', '[', ']'];
}