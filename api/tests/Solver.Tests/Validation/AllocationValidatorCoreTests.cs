using FluentAssertions;
using Solver.Model;
using Xunit;
using static Solver.Tests.Validation.ValidatorScenario;

namespace Solver.Tests.Validation;

public class AllocationValidatorCoreTests
{
    [Fact]
    public void ShouldAcceptValidAllocation()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(2, 2, 2);
        scenario.Seated("A", room, Seat(0, 0, 0));
        scenario.Seated("B", room, Seat(0, 0, 1));
        scenario.Seated("C", room, Seat(1, 1, 1));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeTrue();
        report.Violations.Should().BeEmpty();
        report.SoftPenalty.Should().Be(0);
    }

    [Fact]
    public void ShouldReportUnseatedParticipant()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 2, 1);
        scenario.Seated("A", room, Seat(0, 0));
        var missing = scenario.Participant("B");

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeFalse();
        report.Violations.Should().ContainSingle()
            .Which.Should().Match<Violation>(x => x.Kind == ViolationKind.Unseated && x.IsHard && x.ParticipantId == missing);
    }

    [Fact]
    public void ShouldReportParticipantSeatedMoreThanOnce()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 2, 1);
        var twice = scenario.Seated("A", room, Seat(0, 0));
        scenario.Place(twice, room, Seat(0, 1));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeFalse();
        report.Violations.Should().ContainSingle()
            .Which.Should().Match<Violation>(x => x.Kind == ViolationKind.SeatedMoreThanOnce && x.ParticipantId == twice);
    }

    [Fact]
    public void ShouldReportSeatOccupiedMoreThanOnce()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 2, 1);
        scenario.Seated("A", room, Seat(0, 0));
        scenario.Seated("B", room, Seat(0, 0));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeFalse();
        report.Violations.Should().ContainSingle()
            .Which.Should().Match<Violation>(x => x.Kind == ViolationKind.SeatOccupiedMoreThanOnce && x.RoomId == room && x.SeatKey == Seat(0, 0, 0));
    }

    [Fact]
    public void ShouldReportMissingDisabledAndUnknownSeats()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 2, 1, disabled: [Seat(0, 1)]);
        scenario.Seated("A", room, Seat(0, 1));
        scenario.Seated("B", room, Seat(5, 5));
        scenario.Seated("C", Guid.NewGuid(), Seat(0, 0));
        scenario.Place(Guid.NewGuid(), room, Seat(0, 0));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeFalse();
        report.Violations.Where(x => x.Kind == ViolationKind.SeatNotFound).Should().HaveCount(4).And.OnlyContain(x => x.IsHard);
    }

    [Fact]
    public void ShouldReportCapacityExceeded()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 1, 1);
        scenario.Seated("A", room, Seat(0, 0));
        scenario.Seated("B", room, Seat(0, 0));

        // Act
        var report = scenario.Validate();

        // Assert
        report.Violations.Should().Contain(x => x.Kind == ViolationKind.CapacityExceeded && x.RoomId == room && x.IsHard);
    }

    [Fact]
    public void ShouldRequireAccessibleSeatForParticipantsWhoNeedIt()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 2, 1, accessible: [Seat(0, 0)]);
        var rule = scenario.Rule(RuleType.AccessibleSeating, new AccessibleSeatingParameters());
        scenario.Seated("A", room, Seat(0, 0), needsAccessibleSeat: true);
        var wrong = scenario.Seated("B", room, Seat(0, 1), needsAccessibleSeat: true);

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeFalse();
        report.Violations.Should().ContainSingle()
            .Which.Should().Match<Violation>(x => x.Kind == ViolationKind.AccessibilityViolated && x.ParticipantId == wrong && x.RuleId == rule);
        report.PenaltyByRule.Should().NotContainKey(rule);
    }

    [Fact]
    public void ShouldAcceptAccessibleSeatsForEveryone()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 2, 1, accessible: [Seat(0, 0)]);
        scenario.Seated("A", room, Seat(0, 0), needsAccessibleSeat: true);
        scenario.Seated("B", room, Seat(0, 1));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldRespectRoomAndSeatPins()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var first = scenario.Room(1, 2, 1);
        var second = scenario.Room(1, 2, 1);
        var roomPinned = scenario.Seated("A", first, Seat(0, 1));
        var seatPinned = scenario.Seated("B", second, Seat(0, 1));
        scenario.Pin(roomPinned, first).Pin(seatPinned, second, Seat(0, 1));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldReportViolatedRoomPin()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var first = scenario.Room(1, 2, 1);
        var second = scenario.Room(1, 2, 1);
        var pinned = scenario.Seated("A", second, Seat(0, 0));
        scenario.Pin(pinned, first);

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeFalse();
        report.Violations.Should().ContainSingle()
            .Which.Should().Match<Violation>(x => x.Kind == ViolationKind.PinViolated && x.ParticipantId == pinned && x.RoomId == second);
    }

    [Fact]
    public void ShouldReportViolatedSeatPin()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 2, 1);
        var pinned = scenario.Seated("A", room, Seat(0, 0));
        scenario.Pin(pinned, room, Seat(0, 1));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeFalse();
        report.Violations.Should().ContainSingle()
            .Which.Should().Match<Violation>(x => x.Kind == ViolationKind.PinViolated && x.SeatKey == Seat(0, 0));
    }
}