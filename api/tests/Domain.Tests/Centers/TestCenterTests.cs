using Domain.Centers;
using Domain.Sessions;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Centers;

public class TestCenterTests
{
    private static readonly GeoPoint Location = GeoPoint.From(50.45, 30.52);

    [Fact]
    public void ShouldCreateAndUpdateCenter()
    {
        // Arrange
        var center = TestCenter.New(TestCenterId.New(), ExamSessionId.New(), " Ліцей ", " вул. Хрещатик ", Location, null, TestCenterConstraints.DefaultCostPerRoom, true);

        // Act
        center.UpdateDetails("Ліцей 1", "вул. Січових Стрільців", GeoPoint.From(49.84, 24.03), "place", 2m, false);

        // Assert
        center.Name.Should().Be("Ліцей 1");
        center.Location.Latitude.Should().Be(49.84);
        center.CostPerRoom.Should().Be(2m);
        center.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ShouldRejectNegativeCost()
    {
        // Act
        var act = () => TestCenter.New(TestCenterId.New(), ExamSessionId.New(), "Ліцей", string.Empty, Location, null, -1m, true);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}