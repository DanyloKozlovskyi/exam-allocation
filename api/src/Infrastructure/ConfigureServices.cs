using Application.Common.Interfaces;
using Infrastructure.Caching;
using Infrastructure.DateTimeProvider;
using Infrastructure.ImportExport;
using Infrastructure.Persistence;
using Infrastructure.Solving;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ConfigureServices
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IImportCache, InMemoryImportCache>();
        services.AddPersistenceServices(configuration);
        services.AddImportExportServices();
        services.AddSolvingServices();
    }
}