using Domain.Centers;
using Domain.Rooms;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Rooms;

public class RoomTests
{
    private const string RoomName = "Ауд. 214";

    [Fact]
    public void ShouldComputeCapacityOnCreate()
    {
        // Arrange
        var layout = RoomLayout.Create(1, 2, 2, [], [], ["r0c0s0"]);

        // Act
        var room = Room.New(RoomId.New(), TestCenterId.New(), $"  {RoomName} ", layout, Seats(layout), null, true);

        // Assert
        room.Name.Should().Be(RoomName);
        room.Capacity.Should().Be(4);
        room.AccessibleCapacity.Should().Be(1);
        room.Seats.Should().OnlyContain(x => x.RoomId == room.Id);
    }

    [Fact]
    public void ShouldKeepSurvivingSeatIdsWhenLayoutChanges()
    {
        // Arrange
        var layout = RoomLayout.Create(1, 2, 2, [], [], []);
        var room = Room.New(RoomId.New(), TestCenterId.New(), RoomName, layout, Seats(layout), null, true);
        var keptSeat = room.Seats.Single(x => x.Key == SeatKey.From(0, 0, 0));
        var smaller = RoomLayout.Create(1, 2, 2, [], ["r0c1s0", "r0c1s1"], ["r0c0s0"]);

        // Act
        var removed = room.SeatsRemovedBy(Seats(smaller));
        room.ChangeLayout(smaller, Seats(smaller));

        // Assert
        removed.Select(x => x.Key.Value).Should().BeEquivalentTo("r0c1s0", "r0c1s1");
        room.Layout.Should().Be(smaller);
        room.Capacity.Should().Be(2);
        room.AccessibleCapacity.Should().Be(1);
        room.Seats.Should().Contain(keptSeat);
        keptSeat.IsAccessible.Should().BeTrue();
    }

    [Fact]
    public void ShouldRejectDuplicateSeatKeys()
    {
        // Arrange
        var layout = RoomLayout.Create(1, 1, 1, [], [], []);
        var key = SeatKey.From(0, 0, 0);

        // Act
        var act = () => Room.New(RoomId.New(), TestCenterId.New(), RoomName, layout, [(key, false), (key, true)], null, true);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldValidateDetails()
    {
        // Arrange
        var layout = RoomLayout.Create(1, 1, 1, [], [], []);
        var room = Room.New(RoomId.New(), TestCenterId.New(), RoomName, layout, Seats(layout), null, true);

        // Act
        var emptyName = () => room.UpdateDetails(" ", null, true);
        var tooLong = () => room.UpdateDetails(new string('a', RoomConstraints.NameMaxLength + 1), null, true);
        var negativeCost = () => room.UpdateDetails(RoomName, -1m, true);
        room.UpdateDetails(RoomName, 2m, false);

        // Assert
        emptyName.Should().Throw<ArgumentException>();
        tooLong.Should().Throw<ArgumentOutOfRangeException>();
        negativeCost.Should().Throw<ArgumentOutOfRangeException>();
        room.CostOverride.Should().Be(2m);
        room.IsActive.Should().BeFalse();
    }

    private static IReadOnlyList<(SeatKey Key, bool IsAccessible)> Seats(RoomLayout layout)
        => layout.EnabledSeatKeys().Select(x => (x, layout.AccessibleSeats.Contains(x.Value))).ToList();
}