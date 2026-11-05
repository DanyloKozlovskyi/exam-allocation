using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.ValueObjects;

public class SeatKeyTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(5, 4, 1)]
    [InlineData(12, 29, 2)]
    public void ShouldRoundTripThroughString(int row, int column, int seat)
    {
        // Arrange
        var key = SeatKey.From(row, column, seat);

        // Act
        var parsed = SeatKey.Parse(key.Value);

        // Assert
        parsed.Should().Be(key);
        parsed.Row.Should().Be(row);
        parsed.Column.Should().Be(column);
        parsed.Seat.Should().Be(seat);
    }

    [Theory]
    [InlineData("")]
    [InlineData("r1c2")]
    [InlineData("x1c2s3")]
    [InlineData("r-1c2s3")]
    public void ShouldRejectInvalidKeys(string value)
    {
        // Act
        var parsed = SeatKey.TryParse(value, out var key);

        // Assert
        parsed.Should().BeFalse();
        key.Should().BeNull();
    }
}