using Domain.Centers;
using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Rooms;

public class Room : IEntity<RoomId>, IVersioned
{
    private Room(RoomId id, TestCenterId centerId, string name, RoomLayout layout, decimal? costOverride, bool isActive)
    {
        Id = id;
        CenterId = centerId;
        Name = name;
        Layout = layout;
        CostOverride = costOverride;
        IsActive = isActive;
    }

    private Room()
    {
        Id = RoomId.Empty();
        CenterId = TestCenterId.Empty();
        Name = string.Empty;
        Layout = new RoomLayout();
    }

    public RoomId Id { get; private set; }

    public TestCenterId CenterId { get; private set; }

    public string Name { get; private set; }

    public RoomLayout Layout { get; private set; }

    public int Capacity { get; private set; }

    public int AccessibleCapacity { get; private set; }

    public decimal? CostOverride { get; private set; }

    public bool IsActive { get; private set; }

    public uint Version { get; private set; }

    public TestCenter? Center { get; private set; }

    public ICollection<Seat> Seats { get; private set; } = [];

    /// <summary>Creates a room; <paramref name="seatKeys"/> come from the solver's seat generator for <paramref name="layout"/>.</summary>
    public static Room New(
        RoomId id,
        TestCenterId centerId,
        string name,
        RoomLayout layout,
        IReadOnlyList<(SeatKey Key, bool IsAccessible)> seatKeys,
        decimal? costOverride,
        bool isActive)
    {
        Validate(name, costOverride);
        var room = new Room(id, centerId, name.Trim(), layout, costOverride, isActive);
        room.ReplaceSeats(seatKeys);
        return room;
    }

    public void UpdateDetails(string name, decimal? costOverride, bool isActive)
    {
        Validate(name, costOverride);
        Name = name.Trim();
        CostOverride = costOverride;
        IsActive = isActive;
    }

    public void ChangeLayout(RoomLayout layout, IReadOnlyList<(SeatKey Key, bool IsAccessible)> seatKeys)
    {
        Guard.NotNull(layout, nameof(layout));
        Layout = layout;
        ReplaceSeats(seatKeys);
    }

    private static void Validate(string name, decimal? costOverride)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));
        Guard.MaxLength(name, RoomConstraints.NameMaxLength, nameof(name));
        if (costOverride is { } cost)
        {
            Guard.GreaterThanOrEqual(cost, RoomConstraints.MinCost, nameof(costOverride));
        }
    }

    private void ReplaceSeats(IReadOnlyList<(SeatKey Key, bool IsAccessible)> seatKeys)
    {
        Seats.Clear();
        foreach (var (key, isAccessible) in seatKeys)
        {
            Seats.Add(Seat.New(SeatId.New(), Id, key, isAccessible));
        }

        Capacity = Seats.Count;
        AccessibleCapacity = Seats.Count(x => x.IsAccessible);
    }
}