using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Tests.Common;

public class IntegrationTestWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestSettingsFile = "appsettings.Test.json";
    private const string PostgresImage = "postgres:17";
    private const string Database = "exam_allocation_tests";
    private const string Username = "postgres";
    private const string Password = "postgres";

    private readonly PostgreSqlContainer dbContainer = new PostgreSqlBuilder(PostgresImage)
        .WithDatabase(Database)
        .WithUsername(Username)
        .WithPassword(Password)
        .Build();

    public Task InitializeAsync()
    {
        return dbContainer.StartAsync();
    }

    public new Task DisposeAsync()
    {
        return dbContainer.DisposeAsync().AsTask();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseConfiguration (not ConfigureAppConfiguration): Program binds ApplicationSettings eagerly from
        // builder.Configuration, before deferred app-configuration callbacks of the factory would run.
        var testConfiguration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(TestSettingsFile, optional: true)
            .AddEnvironmentVariables()
            .Build();
        builder.UseConfiguration(testConfiguration);
        builder.ConfigureTestServices(RegisterDatabase);
    }

    private void RegisterDatabase(IServiceCollection services)
    {
        services.RemoveServiceByType(typeof(DbContextOptions<ApplicationDbContext>));

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(dbContainer.GetConnectionString());
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<ApplicationDbContext>(options => options.UseApplicationDatabase(dataSource));
    }
}