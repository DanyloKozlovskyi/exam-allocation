using System.Globalization;

namespace Solver.Model;

/// <summary>Seat key format shared with Domain.SeatKey: r{row}c{column}s{seat}, 0-based.</summary>
public static class SeatKeyFormat
{
    public const string RowPrefix = "r";
    public const string ColumnPrefix = "c";
    public const string SeatPrefix = "s";

    public static string Format(int row, int column, int seat)
        => string.Create(CultureInfo.InvariantCulture, $"{RowPrefix}{row}{ColumnPrefix}{column}{SeatPrefix}{seat}");

    /// <summary>Parses a key produced by <see cref="Format"/> back into (row, column, seat).</summary>
    public static bool TryParse(string? key, out int row, out int column, out int seat)
    {
        row = column = seat = 0;
        if (string.IsNullOrEmpty(key) || !key.StartsWith(SeatKeyFormat.RowPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var columnAt = key.IndexOf(SeatKeyFormat.ColumnPrefix, SeatKeyFormat.RowPrefix.Length, StringComparison.Ordinal);
        if (columnAt < 0)
        {
            return false;
        }

        var seatAt = key.IndexOf(SeatKeyFormat.SeatPrefix, columnAt + SeatKeyFormat.ColumnPrefix.Length, StringComparison.Ordinal);
        if (seatAt < 0)
        {
            return false;
        }

        var rowText = key.AsSpan(SeatKeyFormat.RowPrefix.Length, columnAt - SeatKeyFormat.RowPrefix.Length);
        var columnStart = columnAt + SeatKeyFormat.ColumnPrefix.Length;
        var columnText = key.AsSpan(columnStart, seatAt - columnStart);
        var seatText = key.AsSpan(seatAt + SeatKeyFormat.SeatPrefix.Length);

        return int.TryParse(rowText, NumberStyles.None, CultureInfo.InvariantCulture, out row)
            && int.TryParse(columnText, NumberStyles.None, CultureInfo.InvariantCulture, out column)
            && int.TryParse(seatText, NumberStyles.None, CultureInfo.InvariantCulture, out seat);
    }
}