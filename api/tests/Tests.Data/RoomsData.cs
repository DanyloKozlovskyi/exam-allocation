using Domain.Centers;
using Domain.Rooms;
using Domain.ValueObjects;

namespace Tests.Data;

public static class RoomsData
{
    public const string MainRoomName = "Ауд. 101";
    public const string SecondRoomName = "Ауд. 102";
    public const int Rows = 2;
    public const int Columns = 3;
    public const int SeatsPerDesk = 2;
    public const string AccessibleSeat = "r0c0s0";

    /// <summary>2 x 3 desks, 2 seats each (12 seats), first seat accessible.</summary>
    public static RoomLayout MainLayout()
        => RoomLayout.Create(Rows, Columns, SeatsPerDesk, [], [], [AccessibleSeat]);

    public static Room MainRoom(TestCenterId centerId) => Create(centerId, MainRoomName, MainLayout());

    public static Room SecondRoom(TestCenterId centerId) => Create(centerId, SecondRoomName, MainLayout());

    public static Room Create(TestCenterId centerId, string name, RoomLayout layout)
        => Room.New(RoomId.New(), centerId, name, layout, Seats(layout), null, true);

    /// <summary>Seats as the solver's seat generator produces them (enabled keys, accessibility from the layout).</summary>
    public static IReadOnlyList<(SeatKey Key, bool IsAccessible)> Seats(RoomLayout layout)
        => layout.EnabledSeatKeys().Select(x => (x, layout.AccessibleSeats.Contains(x.Value))).ToList();
}