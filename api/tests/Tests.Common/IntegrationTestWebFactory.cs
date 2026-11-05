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
        builder.ConfigureTestServices(RegisterDatabase)
            .ConfigureAppConfiguration((_, config) =>
            {
                config
                    .AddJsonFile(TestSettingsFile, optional: true)
                    .AddEnvironmentVariables();
            });
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