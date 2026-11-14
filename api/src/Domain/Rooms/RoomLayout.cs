using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Rooms;

/// <summary>Grid layout of a room; stored as jsonb.</summary>
public sealed record RoomLayout
{
    public const int MinSize = 1;
    public const int MaxRows = 50;
    public const int MaxColumns = 30;
    public const int MinSeatsPerDesk = 1;
    public const int MaxSeatsPerDesk = 3;

    public int Rows { get; init; }

    public int Columns { get; init; }

    public int SeatsPerDesk { get; init; }

    public IReadOnlyList<int> AislesAfterColumns { get; init; } = [];

    public IReadOnlyList<string> DisabledSeats { get; init; } = [];

    public IReadOnlyList<string> AccessibleSeats { get; init; } = [];

    public static RoomLayout Create(
        int rows,
        int columns,
        int seatsPerDesk,
        IReadOnlyList<int> aislesAfterColumns,
        IReadOnlyList<string> disabledSeats,
        IReadOnlyList<string> accessibleSeats)
    {
        Guard.InRange(rows, MinSize, MaxRows, nameof(rows));
        Guard.InRange(columns, MinSize, MaxColumns, nameof(columns));
        Guard.InRange(seatsPerDesk, MinSeatsPerDesk, MaxSeatsPerDesk, nameof(seatsPerDesk));
        Guard.NotNull(aislesAfterColumns, nameof(aislesAfterColumns));
        Guard.NotNull(disabledSeats, nameof(disabledSeats));
        Guard.NotNull(accessibleSeats, nameof(accessibleSeats));
        foreach (var aisle in aislesAfterColumns)
        {
            Guard.InRange(aisle, 0, columns - 2, nameof(aislesAfterColumns));
        }

        return new RoomLayout
        {
            Rows = rows,
            Columns = columns,
            SeatsPerDesk = seatsPerDesk,
            AislesAfterColumns = aislesAfterColumns.Distinct().Order().ToList(),
            DisabledSeats = NormalizeSeatKeys(disabledSeats, rows, columns, seatsPerDesk, nameof(disabledSeats)),
            AccessibleSeats = NormalizeSeatKeys(accessibleSeats, rows, columns, seatsPerDesk, nameof(accessibleSeats)),
        };
    }

    /// <summary>True when <paramref name="key"/> lies inside the grid of this layout.</summary>
    public bool Contains(SeatKey key)
        => IsInside(key, Rows, Columns, SeatsPerDesk);

    /// <summary>All seat keys of the grid that are not disabled, in row/column/seat order.</summary>
    public IReadOnlyList<SeatKey> EnabledSeatKeys()
    {
        var disabled = DisabledSeats.ToHashSet(StringComparer.Ordinal);
        var keys = new List<SeatKey>(Rows * Columns * SeatsPerDesk);
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                for (var seat = 0; seat < SeatsPerDesk; seat++)
                {
                    var key = SeatKey.From(row, column, seat);
                    if (!disabled.Contains(key.Value))
                    {
                        keys.Add(key);
                    }
                }
            }
        }

        return keys;
    }

    private static bool IsInside(SeatKey key, int rows, int columns, int seatsPerDesk)
        => key.Row < rows && key.Column < columns && key.Seat < seatsPerDesk;

    private static List<string> NormalizeSeatKeys(IReadOnlyList<string> keys, int rows, int columns, int seatsPerDesk, string name)
    {
        var result = new List<string>(keys.Count);
        foreach (var raw in keys)
        {
            Guard.IsTrue(SeatKey.TryParse(raw, out var key), name, $"Invalid seat key '{raw}'");
            Guard.IsTrue(IsInside(key!, rows, columns, seatsPerDesk), name, $"Seat key '{raw}' is outside of the layout");
            result.Add(key!.Value);
        }

        return result.Distinct(StringComparer.Ordinal).ToList();
    }
}