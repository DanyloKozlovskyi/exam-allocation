using Domain.Participants;
using Domain.Pins;
using Domain.Rooms;
using Domain.Sessions;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Pins;

public class PinTests
{
    [Fact]
    public void ShouldMovePinToAnotherRoomAndSeat()
    {
        // Arrange
        var pin = Pin.New(PinId.New(), ExamSessionId.New(), ParticipantId.New(), RoomId.New(), null);
        var roomId = RoomId.New();
        var seatId = SeatId.New();

        // Act
        pin.MoveTo(roomId, seatId);

        // Assert
        pin.RoomId.Should().Be(roomId);
        pin.SeatId.Should().Be(seatId);
    }
}