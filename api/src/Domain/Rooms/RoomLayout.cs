using Domain.Common;

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
        return new RoomLayout
        {
            Rows = rows,
            Columns = columns,
            SeatsPerDesk = seatsPerDesk,
            AislesAfterColumns = aislesAfterColumns.Distinct().Order().ToList(),
            DisabledSeats = disabledSeats.Distinct().ToList(),
            AccessibleSeats = accessibleSeats.Distinct().ToList(),
        };
    }
}