namespace Domain.Rooms;

public record SeatId(Guid Value)
{
    public static SeatId Empty() => new(Guid.Empty);

    public static SeatId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}