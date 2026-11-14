using System.Net;
using Api.Constants;
using Api.Dtos;
using Api.Tests.Integration.Sessions;
using Application.Common.Errors;
using Domain.Centers;
using Domain.Participants;
using Domain.Pins;
using Domain.Rooms;
using Domain.Sessions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using static Api.Tests.Integration.Sessions.ApiTestHelpers;

namespace Api.Tests.Integration.Pins;

public class PinsControllerTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const string SeatKey = "r1c2s1";
    private readonly ExamSession session = ExamSessionsData.MainSession(DateTimeOffset.UtcNow);
    private Room room = null!;
    private Room secondRoom = null!;
    private Participant ivanenko = null!;
    private Participant petrenko = null!;

    [Fact]
    public async Task ShouldPinParticipantToRoom()
    {
        // Act
        var response = await Client.PostJson(CreateUrl(), new PinDto(null, ivanenko.Id.Value, room.Id.Value, null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pin = await response.ToResponseModel<PinDto>();
        pin.SeatKey.Should().BeNull();
        var stored = await Context.Pins.AsNoTracking().SingleAsync(x => x.ParticipantId == ivanenko.Id);
        stored.RoomId.Should().Be(room.Id);
        stored.SeatId.Should().BeNull();
    }

    [Fact]
    public async Task ShouldPinParticipantToSeat()
    {
        // Act
        var response = await Client.PostJson(CreateUrl(), new PinDto(null, ivanenko.Id.Value, room.Id.Value, SeatKey));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pin = await response.ToResponseModel<PinDto>();
        pin.SeatKey.Should().Be(SeatKey);
        var stored = await Context.Pins.AsNoTracking().SingleAsync(x => x.ParticipantId == ivanenko.Id);
        stored.SeatId.Should().Be(room.Seats.Single(x => x.Key.Value == SeatKey).Id);
    }

    [Fact]
    public async Task ShouldMoveExistingPin()
    {
        // Arrange
        var existing = PinsData.SeatPin(session.Id, ivanenko.Id, room, SeatKey);
        Context.Pins.Add(existing);
        await SaveChangesAsync();

        // Act
        var response = await Client.PostJson(CreateUrl(), new PinDto(null, ivanenko.Id.Value, secondRoom.Id.Value, null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pin = await response.ToResponseModel<PinDto>();
        pin.Id.Should().Be(existing.Id.Value);
        var stored = await Context.Pins.AsNoTracking().SingleAsync(x => x.ParticipantId == ivanenko.Id);
        stored.RoomId.Should().Be(secondRoom.Id);
        stored.SeatId.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotPinToSeatTakenByAnotherParticipant()
    {
        // Arrange
        Context.Pins.Add(PinsData.SeatPin(session.Id, petrenko.Id, room, SeatKey));
        await SaveChangesAsync();

        // Act
        var response = await Client.PostJson(CreateUrl(), new PinDto(null, ivanenko.Id.Value, room.Id.Value, SeatKey));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.PinConflict);
    }

    [Fact]
    public async Task ShouldReturnNotFoundForSeatOutsideOfRoom()
    {
        // Act
        var response = await Client.PostJson(CreateUrl(), new PinDto(null, ivanenko.Id.Value, room.Id.Value, "r9c9s0"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.SeatNotFound);
    }

    [Fact]
    public async Task ShouldReturnNotFoundForUnknownParticipantOrRoom()
    {
        // Act
        var unknownParticipant = await Client.PostJson(CreateUrl(), new PinDto(null, Guid.NewGuid(), room.Id.Value, null));
        var unknownRoom = await Client.PostJson(CreateUrl(), new PinDto(null, ivanenko.Id.Value, Guid.NewGuid(), null));

        // Assert
        unknownParticipant.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await unknownParticipant.ReadErrorCode()).Should().Be(ErrorCode.ParticipantNotFound);
        unknownRoom.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await unknownRoom.ReadErrorCode()).Should().Be(ErrorCode.RoomNotFound);
    }

    [Fact]
    public async Task ShouldNotPinToRoomOfAnotherSession()
    {
        // Arrange
        var other = ExamSessionsData.SecondSession(DateTimeOffset.UtcNow);
        var otherCenter = TestCentersData.MainCenter(other.Id);
        var otherRoom = RoomsData.MainRoom(otherCenter.Id);
        Context.ExamSessions.Add(other);
        Context.TestCenters.Add(otherCenter);
        Context.Rooms.Add(otherRoom);
        await SaveChangesAsync();

        // Act
        var response = await Client.PostJson(CreateUrl(), new PinDto(null, ivanenko.Id.Value, otherRoom.Id.Value, null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.RoomNotFound);
    }

    [Fact]
    public async Task ShouldRejectMalformedSeatKey()
    {
        // Act
        var response = await Client.PostJson(CreateUrl(), new PinDto(null, ivanenko.Id.Value, room.Id.Value, "seat-1"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ValidationFailed);
    }

    [Fact]
    public async Task ShouldGetAllPinsWithSeatKeys()
    {
        // Arrange
        Context.Pins.AddRange(
            PinsData.SeatPin(session.Id, ivanenko.Id, room, SeatKey),
            PinsData.RoomPin(session.Id, petrenko.Id, secondRoom.Id));
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Pins.List, (SessionParam, session.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pins = await response.ToResponseModel<List<PinDto>>();
        pins.Should().HaveCount(2);
        pins.Single(x => x.ParticipantId == ivanenko.Id.Value).SeatKey.Should().Be(SeatKey);
        pins.Single(x => x.ParticipantId == petrenko.Id.Value).SeatKey.Should().BeNull();
    }

    [Fact]
    public async Task ShouldDeletePin()
    {
        // Arrange
        Context.Pins.Add(PinsData.RoomPin(session.Id, ivanenko.Id, room.Id));
        await SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync(Route(ApiRoutes.Pins.ByParticipant, (SessionParam, session.Id.Value), (ParticipantParam, ivanenko.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Context.Pins.AnyAsync(x => x.ParticipantId == ivanenko.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenDeletingMissingPin()
    {
        // Act
        var response = await Client.DeleteAsync(Route(ApiRoutes.Pins.ByParticipant, (SessionParam, session.Id.Value), (ParticipantParam, ivanenko.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.PinNotFound);
    }

    public async Task InitializeAsync()
    {
        var center = TestCentersData.MainCenter(session.Id);
        room = RoomsData.MainRoom(center.Id);
        secondRoom = RoomsData.SecondRoom(center.Id);
        ivanenko = ParticipantsData.Ivanenko(session.Id);
        petrenko = ParticipantsData.Petrenko(session.Id);
        Context.ExamSessions.Add(session);
        Context.TestCenters.Add(center);
        Context.Rooms.AddRange(room, secondRoom);
        Context.Participants.AddRange(ivanenko, petrenko);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.ChangeTracker.Clear();
        await Context.ExamSessions.ExecuteDeleteAsync();
    }

    private string CreateUrl() => Route(ApiRoutes.Pins.Create, (SessionParam, session.Id.Value));
}