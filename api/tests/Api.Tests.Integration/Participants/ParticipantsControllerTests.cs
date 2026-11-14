using System.Net;
using Api.Constants;
using Api.Dtos;
using Api.Tests.Integration.Sessions;
using Application.Common.Errors;
using Application.Common.Models;
using Application.Participants.Models;
using Domain.Participants;
using Domain.Sessions;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using static Api.Tests.Integration.Sessions.ApiTestHelpers;

namespace Api.Tests.Integration.Participants;

public class ParticipantsControllerTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const string NewExternalId = "2001";
    private readonly ExamSession session = ExamSessionsData.MainSession(DateTimeOffset.UtcNow);
    private IReadOnlyList<Participant> participants = [];

    private Participant Ivanenko => participants[0];

    [Fact]
    public async Task ShouldPageAndSortParticipants()
    {
        // Act
        var response = await Client.GetAsync(PageUrl($"page=2&pageSize=2&sortBy={ParticipantSortField.LastName}&sortDirection={SortDirection.Descending}"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.ToResponseModel<PagedDto<ParticipantDto>>();
        page.Total.Should().Be(3);
        page.Page.Should().Be(2);
        page.Items.Select(x => x.LastName).Should().Equal("Іваненко");
    }

    [Fact]
    public async Task ShouldSearchCaseInsensitively()
    {
        // Act
        var response = await Client.GetAsync(PageUrl("search=ПЕТР"));

        // Assert
        var page = await response.ToResponseModel<PagedDto<ParticipantDto>>();
        page.Items.Should().ContainSingle().Which.LastName.Should().Be("Петренко");
    }

    [Fact]
    public async Task ShouldFilterByGroupSchoolAndAccessibility()
    {
        // Act
        var byGroup = await (await Client.GetAsync(PageUrl($"group={Uri.EscapeDataString(ParticipantsData.GroupA)}&school={Uri.EscapeDataString(ParticipantsData.SchoolOne)}")))
            .ToResponseModel<PagedDto<ParticipantDto>>();
        var accessible = await (await Client.GetAsync(PageUrl("needsAccessibleSeat=true"))).ToResponseModel<PagedDto<ParticipantDto>>();

        // Assert
        byGroup.Items.Should().ContainSingle().Which.LastName.Should().Be("Іваненко");
        accessible.Items.Should().ContainSingle().Which.LastName.Should().Be("Петренко");
    }

    [Fact]
    public async Task ShouldGetParticipantById()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Participants.ById, (SessionParam, session.Id.Value), (ParticipantParam, Ivanenko.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var participant = await response.ToResponseModel<ParticipantDto>();
        participant.Attributes.Should().ContainKey(ParticipantsData.LanguageKey);
    }

    [Fact]
    public async Task ShouldReturnNotFoundForUnknownParticipant()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Participants.ById, (SessionParam, session.Id.Value), (ParticipantParam, Guid.NewGuid())));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldCreateParticipantWithNormalizedAttributes()
    {
        // Arrange
        var request = Request(NewExternalId, new Dictionary<string, string> { ["Іноземна Мова"] = "німецька" });

        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Participants.Create, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await response.ToResponseModel<ParticipantDto>();
        var stored = await Context.Participants.AsNoTracking().SingleAsync(x => x.Id == new ParticipantId(created.Id!.Value));
        stored.Attributes.Should().Equal(new Dictionary<string, string> { ["іноземна_мова"] = "німецька" });
        stored.Group.Should().Be(ParticipantsData.GroupB);
    }

    [Fact]
    public async Task ShouldNotCreateParticipantWithDuplicateExternalId()
    {
        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Participants.Create, (SessionParam, session.Id.Value)), Request(Ivanenko.ExternalId, null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ParticipantAlreadyExists);
    }

    [Fact]
    public async Task ShouldNotCreateParticipantInUnknownSession()
    {
        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Participants.Create, (SessionParam, Guid.NewGuid())), Request(NewExternalId, null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.SessionNotFound);
    }

    [Fact]
    public async Task ShouldRejectAttributeReusingBuiltInKey()
    {
        // Arrange
        var request = Request(NewExternalId, new Dictionary<string, string> { [AttributeKeys.Group] = "x" });

        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Participants.Create, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ValidationFailed);
    }

    [Fact]
    public async Task ShouldUpdateParticipant()
    {
        // Arrange
        var request = Request(Ivanenko.ExternalId, null) with { Id = Ivanenko.Id.Value, Version = Ivanenko.Version, NeedsAccessibleSeat = true };

        // Act
        var response = await Client.PutJson(Route(ApiRoutes.Participants.Edit, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Context.Participants.AsNoTracking().SingleAsync(x => x.Id == Ivanenko.Id);
        stored.NeedsAccessibleSeat.Should().BeTrue();
        stored.Name.LastName.Should().Be(request.LastName);
        stored.Attributes.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldRejectStaleParticipantVersion()
    {
        // Arrange
        var request = Request(Ivanenko.ExternalId, null) with { Id = Ivanenko.Id.Value, Version = Ivanenko.Version + 1 };

        // Act
        var response = await Client.PutJson(Route(ApiRoutes.Participants.Edit, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ConcurrencyConflict);
    }

    [Fact]
    public async Task ShouldDeleteParticipant()
    {
        // Act
        var response = await Client.DeleteAsync(Route(ApiRoutes.Participants.ById, (SessionParam, session.Id.Value), (ParticipantParam, Ivanenko.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Context.Participants.AnyAsync(x => x.Id == Ivanenko.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenDeletingUnknownParticipant()
    {
        // Act
        var response = await Client.DeleteAsync(Route(ApiRoutes.Participants.ById, (SessionParam, session.Id.Value), (ParticipantParam, Guid.NewGuid())));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ParticipantNotFound);
    }

    [Fact]
    public async Task ShouldDeleteAllParticipantsOfSession()
    {
        // Arrange
        var other = ExamSessionsData.SecondSession(DateTimeOffset.UtcNow);
        Context.ExamSessions.Add(other);
        Context.Participants.Add(ParticipantsData.Ivanenko(other.Id));
        await SaveChangesAsync();

        // Act
        var response = await Client.PostAsync(Route(ApiRoutes.Participants.DeleteAll, (SessionParam, session.Id.Value)), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Context.Participants.AnyAsync(x => x.SessionId == session.Id)).Should().BeFalse();
        (await Context.Participants.AnyAsync(x => x.SessionId == other.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenDeletingAllInUnknownSession()
    {
        // Act
        var response = await Client.PostAsync(Route(ApiRoutes.Participants.DeleteAll, (SessionParam, Guid.NewGuid())), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldSummarizeAttributes()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Participants.Attributes, (SessionParam, session.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var attributes = await response.ToResponseModel<List<AttributeInfoDto>>();
        attributes.Select(x => x.Key).Should().Equal(AttributeKeys.Group, AttributeKeys.School, ParticipantsData.LanguageKey);
        var group = attributes[0];
        group.DistinctValues.Should().Be(2);
        group.MissingCount.Should().Be(0);
        group.SampleValues.Should().Equal(ParticipantsData.GroupA, ParticipantsData.GroupB);
        var language = attributes[2];
        language.DistinctValues.Should().Be(1);
        language.MissingCount.Should().Be(1);
    }

    public async Task InitializeAsync()
    {
        participants = ParticipantsData.All(session.Id);
        Context.ExamSessions.Add(session);
        Context.Participants.AddRange(participants);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.ChangeTracker.Clear();
        await Context.ExamSessions.ExecuteDeleteAsync();
    }

    private static ParticipantDto Request(string? externalId, IReadOnlyDictionary<string, string>? attributes)
        => new(null, null, externalId, "Коваленко", "Олена", null, ParticipantsData.GroupB, null, false, attributes ?? new Dictionary<string, string>(), null);

    private string PageUrl(string query) => Route(ApiRoutes.Participants.List, (SessionParam, session.Id.Value)) + "?" + query;
}