using System.Globalization;
using Solver.Model;

namespace Solver.Layout;

/// <summary>Parses seat keys produced by <see cref="SeatKeyFormat"/> back into (row, column, seat).</summary>
public static class SeatKeyParser
{
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

    /// <summary>Sort key (row, column, seat); unparsable keys sort last.</summary>
    public static (int Row, int Column, int Seat) SortKey(string key)
        => TryParse(key, out var row, out var column, out var seat) ? (row, column, seat) : (int.MaxValue, int.MaxValue, int.MaxValue);
}