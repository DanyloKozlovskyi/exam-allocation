using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Infrastructure.Persistence;

/// <summary>Used by dotnet-ef only; connection string from env EXAM_ALLOCATION_DB or the local docker default.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public const string ConnectionStringEnvironmentVariable = "EXAM_ALLOCATION_DB";
    public const string LocalConnectionString = "Host=localhost;Port=5432;Database=exam_allocation;Username=exam;Password=exam";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable) ?? LocalConnectionString;
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        options.UseApplicationDatabase(dataSourceBuilder.Build());
        return new ApplicationDbContext(options.Options);
    }
}