using Solver.Model;

namespace Solver.Tests.Layout;

/// <summary>Small layout helpers for WS-B tests.</summary>
public static class LayoutFactory
{
    public static RoomLayoutModel RoomLayout(
        int rows,
        int columns,
        int seatsPerDesk,
        IReadOnlyList<int>? aisles = null,
        IEnumerable<string>? disabled = null,
        IEnumerable<string>? accessible = null)
        => new(
            rows,
            columns,
            seatsPerDesk,
            aisles ?? [],
            new HashSet<string>(disabled ?? []),
            new HashSet<string>(accessible ?? []));

    public static string Key(int row, int column, int seat) => SeatKeyFormat.Format(row, column, seat);
}