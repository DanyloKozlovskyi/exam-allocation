using System.Globalization;
using System.Text.RegularExpressions;
using Domain.Common;

namespace Domain.ValueObjects;

/// <summary>Seat identifier inside a room layout, formatted as r{row}c{column}s{seat} (0-based).</summary>
public sealed partial class SeatKey : ValueObject
{
    public const int MaxLength = 20;
    private const string RowPrefix = "r";
    private const string ColumnPrefix = "c";
    private const string SeatPrefix = "s";

    private SeatKey(int row, int column, int seat)
    {
        Row = row;
        Column = column;
        Seat = seat;
    }

    public int Row { get; }

    public int Column { get; }

    public int Seat { get; }

    public string Value => string.Create(CultureInfo.InvariantCulture, $"{RowPrefix}{Row}{ColumnPrefix}{Column}{SeatPrefix}{Seat}");

    public static SeatKey From(int row, int column, int seat)
    {
        Guard.GreaterThanOrEqual(row, 0, nameof(row));
        Guard.GreaterThanOrEqual(column, 0, nameof(column));
        Guard.GreaterThanOrEqual(seat, 0, nameof(seat));
        return new SeatKey(row, column, seat);
    }

    public static SeatKey Parse(string value)
    {
        if (!TryParse(value, out var key))
        {
            throw new FormatException($"Invalid seat key '{value}'");
        }

        return key!;
    }

    public static bool TryParse(string? value, out SeatKey? key)
    {
        key = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = SeatKeyRegex().Match(value);
        if (!match.Success)
        {
            return false;
        }

        key = new SeatKey(
            int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture));
        return true;
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Row;
        yield return Column;
        yield return Seat;
    }

    [GeneratedRegex("^r(\\d+)c(\\d+)s(\\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex SeatKeyRegex();
}