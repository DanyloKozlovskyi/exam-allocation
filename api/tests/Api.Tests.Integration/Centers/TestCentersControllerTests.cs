using System.Net;
using Api.Constants;
using Api.Dtos;
using Api.Tests.Integration.Sessions;
using Application.Common.Errors;
using Domain.Centers;
using Domain.Sessions;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using static Api.Tests.Integration.Sessions.ApiTestHelpers;

namespace Api.Tests.Integration.Centers;

public class TestCentersControllerTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const string NewName = "Новий центр";
    private readonly ExamSession session = ExamSessionsData.MainSession(DateTimeOffset.UtcNow);
    private TestCenter mainCenter = null!;

    [Fact]
    public async Task ShouldGetAllCentersWithRoomTotals()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Centers.List, (SessionParam, session.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var centers = await response.ToResponseModel<List<TestCenterDto>>();
        var center = centers.Should().ContainSingle().Subject;
        center.Name.Should().Be(TestCentersData.MainCenterName);
        center.RoomCount.Should().Be(2);
        center.Capacity.Should().Be(24);
    }

    [Fact]
    public async Task ShouldGetCenterById()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Centers.ById, (SessionParam, session.Id.Value), (CenterParam, mainCenter.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var center = await response.ToResponseModel<TestCenterDto>();
        center.Latitude.Should().Be(TestCentersData.Latitude);
    }

    [Fact]
    public async Task ShouldReturnNotFoundForCenterOfAnotherSession()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Centers.ById, (SessionParam, Guid.NewGuid()), (CenterParam, mainCenter.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldCreateCenterWithDefaultCost()
    {
        // Arrange
        var request = Request(NewName, null);

        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Centers.Create, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await response.ToResponseModel<TestCenterDto>();
        created.CostPerRoom.Should().Be(TestCenterConstraints.DefaultCostPerRoom);
        var stored = await Context.TestCenters.AsNoTracking().SingleAsync(x => x.Id == new TestCenterId(created.Id!.Value));
        stored.Name.Should().Be(NewName);
        stored.SessionId.Should().Be(session.Id);
    }

    [Fact]
    public async Task ShouldNotCreateCenterWithDuplicateName()
    {
        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Centers.Create, (SessionParam, session.Id.Value)), Request(TestCentersData.MainCenterName, null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.TestCenterAlreadyExists);
    }

    [Fact]
    public async Task ShouldNotCreateCenterInUnknownSession()
    {
        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Centers.Create, (SessionParam, Guid.NewGuid())), Request(NewName, null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.SessionNotFound);
    }

    [Fact]
    public async Task ShouldRejectInvalidCoordinates()
    {
        // Arrange
        var request = Request(NewName, null) with { Latitude = GeoPoint.MaxLatitude + 1 };

        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Centers.Create, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ValidationFailed);
    }

    [Fact]
    public async Task ShouldUpdateCenter()
    {
        // Arrange
        var request = Request(NewName, mainCenter.Version) with { Id = mainCenter.Id.Value, CostPerRoom = 3m, IsActive = false };

        // Act
        var response = await Client.PutJson(Route(ApiRoutes.Centers.Edit, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.ToResponseModel<TestCenterDto>();
        updated.RoomCount.Should().Be(2);
        var stored = await Context.TestCenters.AsNoTracking().SingleAsync(x => x.Id == mainCenter.Id);
        stored.Name.Should().Be(NewName);
        stored.CostPerRoom.Should().Be(3m);
        stored.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldRejectStaleCenterVersion()
    {
        // Arrange
        var request = Request(NewName, mainCenter.Version + 1) with { Id = mainCenter.Id.Value };

        // Act
        var response = await Client.PutJson(Route(ApiRoutes.Centers.Edit, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ConcurrencyConflict);
    }

    [Fact]
    public async Task ShouldNotRenameCenterToExistingName()
    {
        // Arrange
        var second = TestCentersData.SecondCenter(session.Id);
        Context.TestCenters.Add(second);
        await SaveChangesAsync();
        var request = Request(TestCentersData.MainCenterName, second.Version) with { Id = second.Id.Value };

        // Act
        var response = await Client.PutJson(Route(ApiRoutes.Centers.Edit, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.TestCenterAlreadyExists);
    }

    [Fact]
    public async Task ShouldDeleteCenterWithRooms()
    {
        // Act
        var response = await Client.DeleteAsync(Route(ApiRoutes.Centers.ById, (SessionParam, session.Id.Value), (CenterParam, mainCenter.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Context.TestCenters.AnyAsync(x => x.Id == mainCenter.Id)).Should().BeFalse();
        (await Context.Rooms.AnyAsync(x => x.CenterId == mainCenter.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenDeletingUnknownCenter()
    {
        // Act
        var response = await Client.DeleteAsync(Route(ApiRoutes.Centers.ById, (SessionParam, session.Id.Value), (CenterParam, Guid.NewGuid())));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.TestCenterNotFound);
    }

    public async Task InitializeAsync()
    {
        mainCenter = TestCentersData.MainCenter(session.Id);
        Context.ExamSessions.Add(session);
        Context.TestCenters.Add(mainCenter);
        Context.Rooms.AddRange(RoomsData.MainRoom(mainCenter.Id), RoomsData.SecondRoom(mainCenter.Id));
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.ChangeTracker.Clear();
        await Context.ExamSessions.ExecuteDeleteAsync();
    }

    private static TestCenterDto Request(string name, uint? version)
        => new(null, null, name, TestCentersData.Address, TestCentersData.Latitude, TestCentersData.Longitude, null, null, true, Version: version);
}