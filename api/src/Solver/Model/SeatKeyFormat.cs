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
}