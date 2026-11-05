using Domain.ValueObjects;

namespace Domain.Rooms;

public class Seat : IEntity<SeatId>
{
    private Seat(SeatId id, RoomId roomId, SeatKey key, bool isAccessible)
    {
        Id = id;
        RoomId = roomId;
        Key = key;
        Row = key.Row;
        Column = key.Column;
        SeatIndex = key.Seat;
        IsAccessible = isAccessible;
    }

    private Seat()
    {
        Id = SeatId.Empty();
        RoomId = RoomId.Empty();
        Key = SeatKey.From(0, 0, 0);
    }

    public SeatId Id { get; private set; }

    public RoomId RoomId { get; private set; }

    public SeatKey Key { get; private set; }

    public int Row { get; private set; }

    public int Column { get; private set; }

    public int SeatIndex { get; private set; }

    public bool IsAccessible { get; private set; }

    public static Seat New(SeatId id, RoomId roomId, SeatKey key, bool isAccessible) => new(id, roomId, key, isAccessible);
}