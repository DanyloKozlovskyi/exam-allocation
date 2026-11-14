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

    /// <summary>
    /// Replaces the layout; seats whose key survives keep their id (pins stay valid),
    /// removed keys are deleted and new keys are added. Capacity is recomputed.
    /// </summary>
    public void ChangeLayout(RoomLayout layout, IReadOnlyList<(SeatKey Key, bool IsAccessible)> seatKeys)
    {
        Guard.NotNull(layout, nameof(layout));
        Layout = layout;
        ReplaceSeats(seatKeys);
    }

    /// <summary>Seats that would disappear if the room switched to <paramref name="seatKeys"/>.</summary>
    public IReadOnlyList<Seat> SeatsRemovedBy(IReadOnlyList<(SeatKey Key, bool IsAccessible)> seatKeys)
    {
        Guard.NotNull(seatKeys, nameof(seatKeys));
        var remaining = seatKeys.Select(x => x.Key).ToHashSet();
        return Seats.Where(x => !remaining.Contains(x.Key)).ToList();
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
        Guard.NotNull(seatKeys, nameof(seatKeys));
        var target = new Dictionary<SeatKey, bool>();
        foreach (var (key, isAccessible) in seatKeys)
        {
            Guard.NotNull(key, nameof(seatKeys));
            Guard.IsTrue(target.TryAdd(key, isAccessible), nameof(seatKeys), $"Duplicate seat key '{key}'");
        }

        foreach (var seat in Seats.Where(x => !target.ContainsKey(x.Key)).ToList())
        {
            Seats.Remove(seat);
        }

        var existing = Seats.ToDictionary(x => x.Key);
        foreach (var (key, isAccessible) in target)
        {
            if (existing.TryGetValue(key, out var seat))
            {
                seat.SetAccessible(isAccessible);
            }
            else
            {
                Seats.Add(Seat.New(SeatId.New(), Id, key, isAccessible));
            }
        }

        Capacity = Seats.Count;
        AccessibleCapacity = Seats.Count(x => x.IsAccessible);
    }
}