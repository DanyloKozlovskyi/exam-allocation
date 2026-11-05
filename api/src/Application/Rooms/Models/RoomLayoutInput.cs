namespace Application.Rooms.Models;

public sealed record RoomLayoutInput(
    int Rows,
    int Columns,
    int SeatsPerDesk,
    IReadOnlyList<int> AislesAfterColumns,
    IReadOnlyList<string> DisabledSeats,
    IReadOnlyList<string> AccessibleSeats);