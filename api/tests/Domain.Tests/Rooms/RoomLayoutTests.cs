using Domain.Rooms;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Rooms;

public class RoomLayoutTests
{
    private const int Rows = 2;
    private const int Columns = 3;
    private const int SeatsPerDesk = 2;

    [Fact]
    public void ShouldCreateNormalizedLayout()
    {
        // Act
        var layout = RoomLayout.Create(Rows, Columns, SeatsPerDesk, [1, 0, 1], ["r0c0s1", "r0c0s1"], ["r1c2s0"]);

        // Assert
        layout.AislesAfterColumns.Should().Equal(0, 1);
        layout.DisabledSeats.Should().Equal("r0c0s1");
        layout.AccessibleSeats.Should().Equal("r1c2s0");
    }

    [Theory]
    [InlineData(0, Columns, SeatsPerDesk)]
    [InlineData(RoomLayout.MaxRows + 1, Columns, SeatsPerDesk)]
    [InlineData(Rows, 0, SeatsPerDesk)]
    [InlineData(Rows, RoomLayout.MaxColumns + 1, SeatsPerDesk)]
    [InlineData(Rows, Columns, 0)]
    [InlineData(Rows, Columns, RoomLayout.MaxSeatsPerDesk + 1)]
    public void ShouldRejectSizesOutOfRange(int rows, int columns, int seatsPerDesk)
    {
        // Act
        var act = () => RoomLayout.Create(rows, columns, seatsPerDesk, [], [], []);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(Columns - 1)]
    public void ShouldRejectAislesOutsideOfColumns(int aisle)
    {
        // Act
        var act = () => RoomLayout.Create(Rows, Columns, SeatsPerDesk, [aisle], [], []);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("r2c0s0")]
    [InlineData("r0c3s0")]
    [InlineData("r0c0s2")]
    public void ShouldRejectUnknownSeatKeys(string key)
    {
        // Act
        var disabled = () => RoomLayout.Create(Rows, Columns, SeatsPerDesk, [], [key], []);
        var accessible = () => RoomLayout.Create(Rows, Columns, SeatsPerDesk, [], [], [key]);

        // Assert
        disabled.Should().Throw<ArgumentException>();
        accessible.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldListEnabledSeatKeysInOrder()
    {
        // Arrange
        var layout = RoomLayout.Create(1, 2, SeatsPerDesk, [], ["r0c1s0"], []);

        // Act
        var keys = layout.EnabledSeatKeys();

        // Assert
        keys.Select(x => x.Value).Should().Equal("r0c0s0", "r0c0s1", "r0c1s1");
        layout.Contains(SeatKey.From(0, 1, 1)).Should().BeTrue();
        layout.Contains(SeatKey.From(1, 0, 0)).Should().BeFalse();
    }
}