using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Infrastructure.Persistence;

public static class ConfigurePersistenceServices
{
    public static void AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString(ConnectionStringNames.Default));
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<ApplicationDbContext>(options => options.UseApplicationDatabase(dataSource));

        services.AddScoped<ApplicationDbContextInitialiser>();
        services.AddScoped<ITransactionRunner, EfTransactionRunner>();
        services.AddRepositories();
    }

    /// <summary>Shared DbContext options (also used by tests and the design-time factory).</summary>
    public static DbContextOptionsBuilder UseApplicationDatabase(this DbContextOptionsBuilder options, NpgsqlDataSource dataSource)
    {
        return options
            .UseNpgsql(dataSource, builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ExamSessionRepository>();
        services.AddScoped<IExamSessionRepository>(provider => provider.GetRequiredService<ExamSessionRepository>());
        services.AddScoped<IExamSessionQueries>(provider => provider.GetRequiredService<ExamSessionRepository>());

        services.AddScoped<TestCenterRepository>();
        services.AddScoped<ITestCenterRepository>(provider => provider.GetRequiredService<TestCenterRepository>());
        services.AddScoped<ITestCenterQueries>(provider => provider.GetRequiredService<TestCenterRepository>());

        services.AddScoped<RoomRepository>();
        services.AddScoped<IRoomRepository>(provider => provider.GetRequiredService<RoomRepository>());
        services.AddScoped<IRoomQueries>(provider => provider.GetRequiredService<RoomRepository>());

        services.AddScoped<ParticipantRepository>();
        services.AddScoped<IParticipantRepository>(provider => provider.GetRequiredService<ParticipantRepository>());
        services.AddScoped<IParticipantQueries>(provider => provider.GetRequiredService<ParticipantRepository>());

        services.AddScoped<RuleRepository>();
        services.AddScoped<IRuleRepository>(provider => provider.GetRequiredService<RuleRepository>());
        services.AddScoped<IRuleQueries>(provider => provider.GetRequiredService<RuleRepository>());

        services.AddScoped<PinRepository>();
        services.AddScoped<IPinRepository>(provider => provider.GetRequiredService<PinRepository>());
        services.AddScoped<IPinQueries>(provider => provider.GetRequiredService<PinRepository>());

        services.AddScoped<SolveRunRepository>();
        services.AddScoped<ISolveRunRepository>(provider => provider.GetRequiredService<SolveRunRepository>());
        services.AddScoped<ISolveRunQueries>(provider => provider.GetRequiredService<SolveRunRepository>());

        services.AddScoped<AllocationRepository>();
        services.AddScoped<IAllocationRepository>(provider => provider.GetRequiredService<AllocationRepository>());
        services.AddScoped<IAllocationQueries>(provider => provider.GetRequiredService<AllocationRepository>());
    }
}