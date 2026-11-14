using System.Net;
using Api.Constants;
using Api.Dtos;
using Api.Tests.Integration.Sessions;
using Application.Common.Errors;
using Domain.Rules;
using Domain.Rules.Parameters;
using Domain.Sessions;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using static Api.Tests.Integration.Sessions.ApiTestHelpers;

namespace Api.Tests.Integration.Rules;

/// <summary>Rule DTOs are polymorphic, so bodies are read with System.Text.Json (<see cref="ApiTestHelpers.ReadJson{T}"/>).</summary>
public class RulesControllerTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly ExamSession session = ExamSessionsData.MainSession(DateTimeOffset.UtcNow);
    private Rule separateGroups = null!;

    [Fact]
    public async Task ShouldGetAllRulesWithTypedParameters()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Rules.List, (SessionParam, session.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rules = await response.ReadJson<List<RuleDto>>();
        rules.Should().HaveCount(2);
        rules.Single(x => x.Type == RuleType.SeparateByAttribute).Parameters
            .Should().Be(new SeparateByAttributeParametersDto(AttributeKeys.Group, RuleScope.Room, 1));
        rules.Single(x => x.Type == RuleType.FillFromFront).Parameters.Should().BeOfType<FillFromFrontParametersDto>();
        rules.Select(x => x.Type.ToString()).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public async Task ShouldGetRuleById()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Rules.ById, (SessionParam, session.Id.Value), (RuleParam, separateGroups.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rule = await response.ReadJson<RuleDto>();
        rule.DisplayName.Should().Be(RulesData.SeparateGroupsName);
        rule.Type.Should().Be(RuleType.SeparateByAttribute);
    }

    [Fact]
    public async Task ShouldReturnNotFoundForUnknownRule()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Rules.ById, (SessionParam, session.Id.Value), (RuleParam, Guid.NewGuid())));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldListRuleTypes()
    {
        // Act
        var response = await Client.GetAsync(Route(ApiRoutes.Rules.RuleTypes));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var types = await response.ReadJson<List<RuleTypeInfoDto>>();
        types.Select(x => x.Type).Should().Equal(Enum.GetValues<RuleType>());
        types.Single(x => x.Type == RuleType.AccessibleSeating).AllowsSoft.Should().BeFalse();
        types.Single(x => x.Type == RuleType.MinRoomFill).AllowsHard.Should().BeFalse();
        types.Single(x => x.Type == RuleType.MinRoomFill).DefaultParameters.Should().Be(new MinRoomFillParametersDto(RuleConstraints.DefaultMinFillRatio));
    }

    [Fact]
    public async Task ShouldCreateRule()
    {
        // Arrange
        var request = Request(new NoAdjacentByAttributeParametersDto(AttributeKeys.School, [AdjacencyKind.SameDesk, AdjacencyKind.FrontBack], true), true);

        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Rules.Create, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await response.ReadJson<RuleDto>();
        created.Type.Should().Be(RuleType.NoAdjacentByAttribute);
        var stored = await Context.Rules.AsNoTracking().SingleAsync(x => x.Id == new RuleId(created.Id!.Value));
        stored.Parameters.Should().BeOfType<NoAdjacentByAttributeParameters>()
            .Which.Adjacency.Should().Equal(AdjacencyKind.SameDesk, AdjacencyKind.FrontBack);
    }

    [Fact]
    public async Task ShouldNotCreateRuleInUnknownSession()
    {
        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Rules.Create, (SessionParam, Guid.NewGuid())), Request(new FillFromFrontParametersDto(), false));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.SessionNotFound);
    }

    public static TheoryData<RuleParametersDto, bool> InvalidRules => new()
    {
        { new SeparateByAttributeParametersDto("Not Normalized", RuleScope.Room, 1), true },
        { new SeparateByAttributeParametersDto(AttributeKeys.Group, RuleScope.Room, 0), true },
        { new NoAdjacentByAttributeParametersDto(AttributeKeys.Group, [], false), true },
        { new NoAdjacentByAttributeParametersDto(AttributeKeys.Group, [AdjacencyKind.Side, AdjacencyKind.Side], false), true },
        { new MinRoomFillParametersDto(RuleConstraints.MaxFillRatio + 1), false },
        { new KeepTogetherInCenterParametersDto(string.Empty), false },
        { new SpreadInRoomParametersDto([]), false },
        { new FillFromFrontParametersDto(), true },
        { new AccessibleSeatingParametersDto(), false },
    };

    [Theory]
    [MemberData(nameof(InvalidRules))]
    public async Task ShouldRejectInvalidRule(RuleParametersDto parameters, bool isHard)
    {
        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Rules.Create, (SessionParam, session.Id.Value)), Request(parameters, isHard));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ValidationFailed);
    }

    [Fact]
    public async Task ShouldRejectWeightOutOfRange()
    {
        // Arrange
        var request = Request(new FillFromFrontParametersDto(), false) with { Weight = RuleConstraints.MaxWeight + 1 };

        // Act
        var response = await Client.PostJson(Route(ApiRoutes.Rules.Create, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldUpdateRuleIncludingType()
    {
        // Arrange
        var request = Request(new KeepTogetherInCenterParametersDto(AttributeKeys.School), false) with
        {
            Id = separateGroups.Id.Value,
            Version = separateGroups.Version,
            IsEnabled = false,
        };

        // Act
        var response = await Client.PutJson(Route(ApiRoutes.Rules.Edit, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Context.Rules.AsNoTracking().SingleAsync(x => x.Id == separateGroups.Id);
        stored.Type.Should().Be(RuleType.KeepTogetherInCenter);
        stored.IsEnabled.Should().BeFalse();
        stored.Parameters.Should().Be(new KeepTogetherInCenterParameters(AttributeKeys.School));
    }

    [Fact]
    public async Task ShouldRejectStaleRuleVersion()
    {
        // Arrange
        var request = Request(new FillFromFrontParametersDto(), false) with { Id = separateGroups.Id.Value, Version = separateGroups.Version + 1 };

        // Act
        var response = await Client.PutJson(Route(ApiRoutes.Rules.Edit, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.ConcurrencyConflict);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenUpdatingUnknownRule()
    {
        // Arrange
        var request = Request(new FillFromFrontParametersDto(), false) with { Id = Guid.NewGuid(), Version = 1 };

        // Act
        var response = await Client.PutJson(Route(ApiRoutes.Rules.Edit, (SessionParam, session.Id.Value)), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadErrorCode()).Should().Be(ErrorCode.RuleNotFound);
    }

    [Fact]
    public async Task ShouldDeleteRule()
    {
        // Act
        var response = await Client.DeleteAsync(Route(ApiRoutes.Rules.ById, (SessionParam, session.Id.Value), (RuleParam, separateGroups.Id.Value)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Context.Rules.AnyAsync(x => x.Id == separateGroups.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenDeletingUnknownRule()
    {
        // Act
        var response = await Client.DeleteAsync(Route(ApiRoutes.Rules.ById, (SessionParam, session.Id.Value), (RuleParam, Guid.NewGuid())));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public async Task InitializeAsync()
    {
        separateGroups = RulesData.SeparateGroups(session.Id);
        Context.ExamSessions.Add(session);
        Context.Rules.AddRange(separateGroups, RulesData.FillFromFront(session.Id));
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.ChangeTracker.Clear();
        await Context.ExamSessions.ExecuteDeleteAsync();
    }

    private static RuleDto Request(RuleParametersDto parameters, bool isHard)
        => new(null, null, parameters.ToDomain().RuleType, isHard, RuleConstraints.DefaultWeight, true, null, parameters);
}