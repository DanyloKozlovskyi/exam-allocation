using FluentAssertions;
using Solver.Layout;
using Xunit;
using static Solver.Tests.Layout.LayoutFactory;

namespace Solver.Tests.Layout;

public class SeatGeneratorTests
{
    private readonly SeatGenerator _generator = new();

    [Fact]
    public void ShouldGenerateSeatsInSeatKeyOrder()
    {
        // Act
        var seats = _generator.Generate(RoomLayout(2, 2, 2));

        // Assert
        seats.Select(s => s.Key).Should().Equal(
            Key(0, 0, 0), Key(0, 0, 1), Key(0, 1, 0), Key(0, 1, 1), Key(1, 0, 0), Key(1, 0, 1), Key(1, 1, 0), Key(1, 1, 1));
        seats[5].Row.Should().Be(1);
        seats[5].Column.Should().Be(0);
        seats[5].SeatIndex.Should().Be(1);
        seats.Should().OnlyContain(s => !s.IsAccessible);
    }

    [Fact]
    public void ShouldSkipDisabledSeatsAndFlagAccessibleSeats()
    {
        // Arrange
        var layout = RoomLayout(2, 3, 1, disabled: [Key(0, 1, 0), Key(1, 2, 0)], accessible: [Key(1, 0, 0)]);

        // Act
        var seats = _generator.Generate(layout);

        // Assert
        seats.Should().HaveCount(4);
        seats.Select(s => s.Key).Should().Equal(Key(0, 0, 0), Key(0, 2, 0), Key(1, 0, 0), Key(1, 1, 0));
        seats.Where(s => s.IsAccessible).Select(s => s.Key).Should().Equal(Key(1, 0, 0));
    }

    [Fact]
    public void ShouldIgnoreDisabledKeysOutsideLayout()
    {
        // Act
        var seats = _generator.Generate(RoomLayout(1, 2, 1, disabled: [Key(5, 5, 5)]));

        // Assert
        seats.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(12, 3, 1)]
    [InlineData(7, 10, 2)]
    public void ShouldRoundTripSeatKeys(int row, int column, int seat)
    {
        // Act
        var parsed = SeatKeyParser.TryParse(Key(row, column, seat), out var r, out var c, out var s);

        // Assert
        parsed.Should().BeTrue();
        (r, c, s).Should().Be((row, column, seat));
    }

    [Theory]
    [InlineData("")]
    [InlineData("x1c1s1")]
    [InlineData("r1c1")]
    [InlineData("r1cxs1")]
    public void ShouldRejectMalformedSeatKeys(string key)
    {
        SeatKeyParser.TryParse(key, out _, out _, out _).Should().BeFalse();
    }
}