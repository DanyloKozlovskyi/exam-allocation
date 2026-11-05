using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Common;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebFactory>
{
    protected BaseIntegrationTest(IntegrationTestWebFactory factory)
    {
        var scope = factory.Services.CreateScope();
        Context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        Server = factory.Server;
    }

    protected ApplicationDbContext Context { get; }

    protected HttpClient Client { get; }

    protected TestServer Server { get; }

    protected async Task<int> SaveChangesAsync()
    {
        var result = await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        return result;
    }
}