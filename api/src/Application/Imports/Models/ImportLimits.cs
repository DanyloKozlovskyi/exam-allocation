namespace Application.Imports.Models;

public static class ImportLimits
{
    public const long BytesPerMegabyte = 1024 * 1024;

    /// <summary>Header row used for the upload preview (1-based, like <see cref="ImportOptions.HeaderRow"/>).</summary>
    public const int DefaultHeaderRow = 1;
}