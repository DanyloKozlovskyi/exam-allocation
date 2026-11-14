using System.Net;
using Api.Constants;
using Api.Dtos;
using Application.Common.Errors;
using Domain.Sessions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using static Api.Tests.Integration.Sessions.ApiTestHelpers;

namespace Api.Tests.Integration.Sessions;

public class SessionsControllerTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const string NewName = "Нова сесія";
    private readonly ExamSession mainSession = ExamSessionsData.MainSession(DateTimeOffset.UtcNow);

    [Fact]
    public async Task ShouldGetAllSessions()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Sessions.List));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sessions = await response.ToResponseModel<List<SessionSummaryDto>>();
        sessions.Should().ContainSingle(x => x.Id == mainSession.Id.Value && x.Name == mainSession.Name);
    }

    [Fact]
    public async Task ShouldGetSessionById()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Sessions.ById, (SessionParam, mainSession.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await response.ToResponseModel<SessionDto>();
        session.Name.Should().Be(mainSession.Name);
        session.Version.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldReturnNotFoundForUnknownSession()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Sessions.ById, (SessionParam, Guid.NewGuid())));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldCreateSession()
    {
        // Arrange
        var request = new SessionDto(null, NewName, new DateOnly(2026, 6, 1), "опис");

        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Sessions.Create), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await response.ToResponseModel<SessionDto>();
        created.Id.Should().NotBeNull();
        var stored = await Context.ExamSessions.AsNoTracking().SingleAsync(x => x.Id == new ExamSessionId(created.Id!.Value));
        stored.Name.Should().Be(NewName);
        stored.ExamDate.Should().Be(request.ExamDate);
    }

    [Fact]
    public async Task ShouldNotCreateSessionWithDuplicateName()
    {
        // Arrange
        var request = new SessionDto(null, mainSession.Name, null, null);

        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Sessions.Create), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.SessionAlreadyExists);
    }

    [Fact]
    public async Task ShouldRejectInvalidSession()
    {
        // Arrange
        var request = new SessionDto(null, new string('a', ExamSessionConstraints.NameMaxLength + 1), null, null);

        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Sessions.Create), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ValidationFailed);
    }

    [Fact]
    public async Task ShouldUpdateSession()
    {
        // Arrange
        var request = new SessionDto(mainSession.Id.Value, NewName, null, "оновлено", Version: mainSession.Version);

        // Act
        var response = await Client.PutJson(Route(ApiRoutes.Sessions.Edit), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.ToResponseModel<SessionDto>();
        updated.Name.Should().Be(NewName);
        updated.Version.Should().NotBe(mainSession.Version);
        var stored = await Context.ExamSessions.AsNoTracking().SingleAsync(x => x.Id == mainSession.Id);
        stored.Description.Should().Be("оновлено");
    }

    [Fact]
    public async Task ShouldRejectStaleSessionVersion()
    {
        // Arrange
        var request = new SessionDto(mainSession.Id.Value, NewName, null, null, Version: mainSession.Version + 1);

        // Act
        var response = await Client.PutJson(Route(ApiRoutes.Sessions.Edit), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ConcurrencyConflict);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenUpdatingUnknownSession()
    {
        // Arrange
        var request = new SessionDto(Guid.NewGuid(), NewName, null, null, Version: 1);

        // Act
        var response = await Client.PutJson(Route(ApiRoutes.Sessions.Edit), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.SessionNotFound);
    }

    [Fact]
    public async Task ShouldDeleteSessionWithChildren()
    {
        // Arrange
        Context.TestCenters.Add(TestCentersData.MainCenter(mainSession.Id));
        Context.Participants.Add(ParticipantsData.Ivanenko(mainSession.Id));
        await SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync(Route(ApiRoutes.Sessions.ById, (SessionParam, mainSession.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Context.ExamSessions.AnyAsync(x => x.Id == mainSession.Id)).Should().BeFalse();
        (await Context.TestCenters.AnyAsync(x => x.SessionId == mainSession.Id)).Should().BeFalse();
        (await Context.Participants.AnyAsync(x => x.SessionId == mainSession.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenDeletingUnknownSession()
    {
        // Act
        var response = await Client.DeleteAsync(Route(ApiRoutes.Sessions.ById, (SessionParam, Guid.NewGuid())));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldReturnTotals()
    {
        // Arrange
        var center = TestCentersData.MainCenter(mainSession.Id);
        var inactiveCenter = TestCentersData.SecondCenter(mainSession.Id);
        inactiveCenter.UpdateDetails(inactiveCenter.Name, inactiveCenter.Address, inactiveCenter.Location, null, inactiveCenter.CostPerRoom, false);
        Context.TestCenters.AddRange(center, inactiveCenter);
        Context.Rooms.AddRange(RoomsData.MainRoom(center.Id), RoomsData.SecondRoom(center.Id), RoomsData.MainRoom(inactiveCenter.Id));
        Context.Participants.AddRange(ParticipantsData.All(mainSession.Id));
        Context.Rules.AddRange(RulesData.SeparateGroups(mainSession.Id), RulesData.FillFromFront(mainSession.Id));
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Sessions.Totals, (SessionParam, mainSession.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var totals = await response.ToResponseModel<SessionTotalsDto>();
        totals.Should().BeEquivalentTo(new SessionTotalsDto(1, 2, 24, 2, 3, 1, 2, 2, false));
    }

    [Fact]
    public async Task ShouldReturnNotFoundTotalsForUnknownSession()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Sessions.Totals, (SessionParam, Guid.NewGuid())));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public async Task InitializeAsync()
    {
        await Context.ExamSessions.AddAsync(mainSession);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.ChangeTracker.Clear();
        await Context.ExamSessions.ExecuteDeleteAsync();
    }
}