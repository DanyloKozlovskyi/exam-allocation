using System.Net;
using Api.Constants;
using Api.Modules;
using FluentAssertions;
using Tests.Common;
using Xunit;

namespace Api.Tests.Integration.Health;

public class HealthTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ShouldReturnHealthy()
    {
        // Act
        var response = await Client.GetAsync(ApiRoutes.Health);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be(ApiEndpointsModule.HealthyResponse);
    }

    [Fact]
    public async Task ShouldApplyMigrations()
    {
        // Act
        var canConnect = await Context.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue();
        Context.ExamSessions.Should().BeEmpty();
    }
}