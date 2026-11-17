using Solver.Model;

namespace Solver.Layout;

/// <summary>Parses seat keys produced by <see cref="SeatKeyFormat"/> back into (row, column, seat).</summary>
public static class SeatKeyParser
{
    public static bool TryParse(string? key, out int row, out int column, out int seat)
        => SeatKeyFormat.TryParse(key, out row, out column, out seat);

    /// <summary>Sort key (row, column, seat); unparsable keys sort last.</summary>
    public static (int Row, int Column, int Seat) SortKey(string key)
        => TryParse(key, out var row, out var column, out var seat) ? (row, column, seat) : (int.MaxValue, int.MaxValue, int.MaxValue);
}