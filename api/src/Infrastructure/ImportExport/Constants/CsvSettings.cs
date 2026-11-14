using System.Text;

namespace Infrastructure.ImportExport.Constants;

public static class CsvSettings
{
    public const char Semicolon = ';';
    public const char Comma = ',';
    public const char Tab = '\t';
    public const char Quote = '"';
    public const char NewLine = '\n';

    /// <summary>Record separator of exported CSV files (CRLF, Excel-friendly).</summary>
    public const string ExportNewLine = "\r\n";
    public const char DefaultDelimiter = Semicolon;

    /// <summary>Windows-1251 (Cyrillic) code page used as fallback for non-UTF-8 files.</summary>
    public const int FallbackCodePage = 1251;

    /// <summary>Number of leading lines inspected for delimiter detection.</summary>
    public const int DelimiterProbeLines = 5;

    public static IReadOnlyList<char> CandidateDelimiters { get; } = [Semicolon, Comma, Tab];

    /// <summary>UTF-8 that throws on invalid bytes (used to detect non-UTF-8 input).</summary>
    public static Encoding StrictUtf8 { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>UTF-8 with BOM for Excel-friendly exports.</summary>
    public static Encoding Utf8WithBom { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
}