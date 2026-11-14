using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Solver.Layout;
using Tests.Common;

namespace Api.Tests.Integration.Rooms.Fakes;

public static class FakeLayoutServices
{
    /// <summary>HTTP client of a factory whose seat generator and adjacency builder are the fakes above.</summary>
    public static HttpClient CreateClientWithFakeLayout(this IntegrationTestWebFactory factory)
        => factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveServiceByType(typeof(ISeatGenerator));
                services.RemoveServiceByType(typeof(IAdjacencyBuilder));
                services.AddSingleton<ISeatGenerator, FakeSeatGenerator>();
                services.AddSingleton<IAdjacencyBuilder, FakeAdjacencyBuilder>();
            }))
            .CreateClient();
}