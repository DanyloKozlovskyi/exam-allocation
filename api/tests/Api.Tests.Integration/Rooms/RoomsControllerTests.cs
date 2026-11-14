using System.Net;
using Api.Constants;
using Api.Dtos;
using Api.Tests.Integration.Rooms.Fakes;
using Api.Tests.Integration.Sessions;
using Application.Common.Errors;
using Domain.Centers;
using Domain.Participants;
using Domain.Pins;
using Domain.Rooms;
using Domain.Rules;
using Domain.Sessions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using static Api.Tests.Integration.Sessions.ApiTestHelpers;

namespace Api.Tests.Integration.Rooms;

public class RoomsControllerTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const string NewName = "Ауд. 301";
    private const int FullCapacity = RoomsData.Rows * RoomsData.Columns * RoomsData.SeatsPerDesk;
    private const string LastSeat = "r1c2s1";
    private readonly HttpClient client = factory.CreateClientWithFakeLayout();
    private readonly ExamSession session = ExamSessionsData.MainSession(DateTimeOffset.UtcNow);
    private TestCenter center = null!;
    private Room mainRoom = null!;

    [Fact]
    public async Task ShouldGetAllRoomsWithoutSeats()
    {
        // Act
        var response = await client.GetAsync(Route(ApiRoutes.Rooms.List, (CenterParam, center.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rooms = await response.ToResponseModel<List<RoomDto>>();
        var room = rooms.Should().ContainSingle().Subject;
        room.Capacity.Should().Be(FullCapacity);
        room.Seats.Should().BeNull();
    }

    [Fact]
    public async Task ShouldGetRoomWithSeats()
    {
        // Act
        var response = await client.GetAsync(Route(ApiRoutes.Rooms.ById, (CenterParam, center.Id.Value), (RoomParam, mainRoom.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var room = await response.ToResponseModel<RoomDto>();
        room.Seats.Should().HaveCount(FullCapacity);
        room.Seats![0].SeatKey.Should().Be(RoomsData.AccessibleSeat);
        room.AccessibleCapacity.Should().Be(1);
    }

    [Fact]
    public async Task ShouldReturnNotFoundForUnknownRoom()
    {
        // Act
        var response = await client.GetAsync(Route(ApiRoutes.Rooms.ById, (CenterParam, center.Id.Value), (RoomParam, Guid.NewGuid())));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldCreateRoomWithGeneratedSeats()
    {
        // Arrange
        var request = Request(NewName, Layout([0], ["r0c0s1"], ["r0c0s0", "r0c1s0"]));

        // Act
        var response = await client.PostJson(Route(ApiRoutes.Rooms.Create, (CenterParam, center.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await response.ToResponseModel<RoomDto>();
        created.Capacity.Should().Be(FullCapacity - 1);
        created.AccessibleCapacity.Should().Be(2);
        var stored = await Context.Rooms.AsNoTracking().Include(x => x.Seats).SingleAsync(x => x.Id == new RoomId(created.Id!.Value));
        stored.Seats.Should().HaveCount(FullCapacity - 1);
        stored.Layout.AislesAfterColumns.Should().Equal(0);
    }

    [Fact]
    public async Task ShouldNotCreateRoomInUnknownCenter()
    {
        // Act
        var response = await client.PostJson(Route(ApiRoutes.Rooms.Create, (CenterParam, Guid.NewGuid())), Request(NewName, Layout([], [], [])));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.TestCenterNotFound);
    }

    [Fact]
    public async Task ShouldNotCreateRoomWithDuplicateName()
    {
        // Act
        var response = await client.PostJson(Route(ApiRoutes.Rooms.Create, (CenterParam, center.Id.Value)), Request(RoomsData.MainRoomName, Layout([], [], [])));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.RoomAlreadyExists);
    }

    [Theory]
    [InlineData(0, RoomsData.Columns, null)]
    [InlineData(RoomsData.Rows, RoomLayout.MaxColumns + 1, null)]
    [InlineData(RoomsData.Rows, RoomsData.Columns, "r9c0s0")]
    public async Task ShouldRejectInvalidLayout(int rows, int columns, string? disabledSeat)
    {
        // Arrange
        var layout = new RoomLayoutDto(rows, columns, RoomsData.SeatsPerDesk, [], disabledSeat is null ? [] : [disabledSeat], []);

        // Act
        var response = await client.PostJson(Route(ApiRoutes.Rooms.Create, (CenterParam, center.Id.Value)), Request(NewName, layout));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ValidationFailed);
    }

    [Fact]
    public async Task ShouldCreateRoomsInBulk()
    {
        // Arrange
        var request = new CreateRoomsBulkDto(Request(string.Empty, Layout([], [], [])), ["Ауд. 401", "Ауд. 402", "Ауд. 403"]);

        // Act
        var response = await client.PostJson(Route(ApiRoutes.Rooms.CreateBulk, (CenterParam, center.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rooms = await response.ToResponseModel<List<RoomDto>>();
        rooms.Should().HaveCount(3).And.OnlyContain(x => x.Capacity == FullCapacity);
        (await Context.Seats.CountAsync(x => rooms.Select(r => new RoomId(r.Id!.Value)).Contains(x.RoomId))).Should().Be(3 * FullCapacity);
    }

    [Fact]
    public async Task ShouldNotCreateRoomsInBulkWithExistingName()
    {
        // Arrange
        var request = new CreateRoomsBulkDto(Request(string.Empty, Layout([], [], [])), ["Ауд. 401", RoomsData.MainRoomName]);

        // Act
        var response = await client.PostJson(Route(ApiRoutes.Rooms.CreateBulk, (CenterParam, center.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Context.Rooms.CountAsync(x => x.CenterId == center.Id)).Should().Be(1);
    }

    [Fact]
    public async Task ShouldRejectDuplicateNamesInBulk()
    {
        // Arrange
        var request = new CreateRoomsBulkDto(Request(string.Empty, Layout([], [], [])), ["Ауд. 401", "Ауд. 401"]);

        // Act
        var response = await client.PostJson(Route(ApiRoutes.Rooms.CreateBulk, (CenterParam, center.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ValidationFailed);
    }

    [Fact]
    public async Task ShouldUpdateRoomKeepingSurvivingSeats()
    {
        // Arrange
        var keptSeatId = mainRoom.Seats.Single(x => x.Key.Value == RoomsData.AccessibleSeat).Id;
        var request = Request(NewName, Layout([1], [LastSeat], [])) with { Id = mainRoom.Id.Value, Version = mainRoom.Version, CostOverride = 2m };

        // Act
        var response = await client.PutJson(Route(ApiRoutes.Rooms.Edit, (CenterParam, center.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.ToResponseModel<RoomDto>();
        updated.Capacity.Should().Be(FullCapacity - 1);
        updated.AccessibleCapacity.Should().Be(0);
        var stored = await Context.Rooms.AsNoTracking().Include(x => x.Seats).SingleAsync(x => x.Id == mainRoom.Id);
        stored.Name.Should().Be(NewName);
        stored.CostOverride.Should().Be(2m);
        stored.Seats.Should().HaveCount(FullCapacity - 1).And.Contain(x => x.Id == keptSeatId && !x.IsAccessible);
    }

    [Fact]
    public async Task ShouldNotRemoveSeatReferencedByPin()
    {
        // Arrange
        var participant = ParticipantsData.Ivanenko(session.Id);
        Context.Participants.Add(participant);
        var pin = PinsData.SeatPin(session.Id, participant.Id, mainRoom, LastSeat);
        Context.Pins.Add(pin);
        await SaveChangesAsync();
        var request = Request(RoomsData.MainRoomName, Layout([], [LastSeat], [])) with { Id = mainRoom.Id.Value, Version = mainRoom.Version };

        // Act
        var response = await client.PutJson(Route(ApiRoutes.Rooms.Edit, (CenterParam, center.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.SeatReferencedByPin);
        (await response.ReadErrorData<List<Guid>>()).Should().Equal(pin.Id.Value);
        (await Context.Seats.CountAsync(x => x.RoomId == mainRoom.Id)).Should().Be(FullCapacity);
    }

    [Fact]
    public async Task ShouldRejectStaleRoomVersion()
    {
        // Arrange
        var request = Request(NewName, Layout([], [], [])) with { Id = mainRoom.Id.Value, Version = mainRoom.Version + 1 };

        // Act
        var response = await client.PutJson(Route(ApiRoutes.Rooms.Edit, (CenterParam, center.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ConcurrencyConflict);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenUpdatingUnknownRoom()
    {
        // Arrange
        var request = Request(NewName, Layout([], [], [])) with { Id = Guid.NewGuid(), Version = 1 };

        // Act
        var response = await client.PutJson(Route(ApiRoutes.Rooms.Edit, (CenterParam, center.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.RoomNotFound);
    }

    [Fact]
    public async Task ShouldDeleteRoomWithSeatsAndPins()
    {
        // Arrange
        var participant = ParticipantsData.Ivanenko(session.Id);
        Context.Participants.Add(participant);
        Context.Pins.Add(PinsData.SeatPin(session.Id, participant.Id, mainRoom, LastSeat));
        await SaveChangesAsync();

        // Act
        var response = await client.DeleteAsync(Route(ApiRoutes.Rooms.ById, (CenterParam, center.Id.Value), (RoomParam, mainRoom.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Context.Rooms.AnyAsync(x => x.Id == mainRoom.Id)).Should().BeFalse();
        (await Context.Seats.AnyAsync(x => x.RoomId == mainRoom.Id)).Should().BeFalse();
        (await Context.Pins.AnyAsync(x => x.RoomId == mainRoom.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenDeletingUnknownRoom()
    {
        // Act
        var response = await client.DeleteAsync(Route(ApiRoutes.Rooms.ById, (CenterParam, center.Id.Value), (RoomParam, Guid.NewGuid())));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldPreviewLayoutWithAdjacencyPerKind()
    {
        // Arrange
        var layout = new RoomLayoutDto(1, 2, 2, [], ["r0c1s1"], ["r0c0s0"]);

        // Act
        var response = await client.PostJson(Route(ApiRoutes.Rooms.PreviewLayout), layout);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await response.ReadJson<RoomLayoutPreviewDto>();
        preview.Capacity.Should().Be(3);
        preview.AccessibleCapacity.Should().Be(1);
        preview.Seats.Select(x => x.SeatKey).Should().Equal("r0c0s0", "r0c0s1", "r0c1s0");
        preview.Adjacency.Select(x => x.Kind).Should().BeEquivalentTo(Enum.GetValues<AdjacencyKind>());
        preview.Adjacency.Single(x => x.Kind == AdjacencyKind.SameDesk).Edges.Should().Equal(new SeatEdgeDto(0, 1));
        (await Context.Rooms.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ShouldRejectInvalidPreviewLayout()
    {
        // Act
        var response = await client.PostJson(Route(ApiRoutes.Rooms.PreviewLayout), new RoomLayoutDto(1, 1, RoomLayout.MaxSeatsPerDesk + 1, [], [], []));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ValidationFailed);
    }

    public async Task InitializeAsync()
    {
        center = TestCentersData.MainCenter(session.Id);
        mainRoom = RoomsData.MainRoom(center.Id);
        Context.ExamSessions.Add(session);
        Context.TestCenters.Add(center);
        Context.Rooms.Add(mainRoom);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.ChangeTracker.Clear();
        await Context.ExamSessions.ExecuteDeleteAsync();
    }

    private static RoomLayoutDto Layout(IReadOnlyList<int> aisles, IReadOnlyList<string> disabled, IReadOnlyList<string> accessible)
        => new(RoomsData.Rows, RoomsData.Columns, RoomsData.SeatsPerDesk, aisles, disabled, accessible);

    private static RoomDto Request(string name, RoomLayoutDto layout) => new(null, null, name, layout, null, true);
}