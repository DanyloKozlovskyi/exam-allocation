using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Common;

public class ValueObjectTests
{
    private const double Latitude = 50.45;
    private const double Longitude = 30.52;

    [Fact]
    public void ShouldCompareGeoPointsByValue()
    {
        // Act
        var first = GeoPoint.From(Latitude, Longitude);
        var second = GeoPoint.From(Latitude, Longitude);

        // Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
        first.Should().NotBe(GeoPoint.From(Longitude, Latitude));
    }

    [Theory]
    [InlineData(GeoPoint.MaxLatitude + 1, 0)]
    [InlineData(GeoPoint.MinLatitude - 1, 0)]
    [InlineData(0, GeoPoint.MaxLongitude + 1)]
    [InlineData(0, GeoPoint.MinLongitude - 1)]
    public void ShouldRejectCoordinatesOutOfRange(double latitude, double longitude)
    {
        // Act
        var act = () => GeoPoint.From(latitude, longitude);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ShouldTrimPersonNameAndBuildSortKey()
    {
        // Act
        var name = PersonName.From(" Шевченко ", "Тарас ", "  ");

        // Assert
        name.LastName.Should().Be("Шевченко");
        name.FirstName.Should().Be("Тарас");
        name.MiddleName.Should().BeNull();
        name.SortKey.Should().Be("Шевченко Тарас");
    }

    [Fact]
    public void ShouldRejectEmptyOrTooLongNameParts()
    {
        // Act
        var empty = () => PersonName.From(" ", "Тарас", null);
        var tooLong = () => PersonName.From(new string('a', PersonName.PartMaxLength + 1), "Тарас", null);

        // Assert
        empty.Should().Throw<ArgumentException>();
        tooLong.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ShouldExposeBuiltInAttributeKeys()
    {
        // Assert
        AttributeKey.Group.Value.Should().Be(AttributeKeys.Group);
        AttributeKey.School.Value.Should().Be(AttributeKeys.School);
        AttributeKey.From(" Group ").Should().Be(AttributeKey.Group);
    }

    [Fact]
    public void ShouldCompareSeatKeysByValue()
    {
        // Assert
        SeatKey.From(1, 2, 0).Should().Be(SeatKey.Parse("r1c2s0"));
        SeatKey.From(1, 2, 0).Value.Should().Be("r1c2s0");
    }
}