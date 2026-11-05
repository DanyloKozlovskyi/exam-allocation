using Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Solving;

public static class ConfigureSolvingServices
{
    public static void AddSolvingServices(this IServiceCollection services)
    {
        services.AddSingleton<ISolveQueue, SolveQueue>();
        services.AddSingleton<ISolveRunCancellation, SolveRunCancellation>();
        services.AddHostedService<SolveWorker>();
    }
}