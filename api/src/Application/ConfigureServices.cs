using System.Reflection;
using Application.Allocations.Services;
using Application.Common.Behaviours;
using Application.Solving.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Solver;

namespace Application;

public static class ConfigureServices
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddSolver();
        services.AddScoped<ISolverInputBuilder, SolverInputBuilder>();
        services.AddScoped<ISolveRunExecutor, SolveRunExecutor>();
        services.AddScoped<ISessionFingerprint, SessionFingerprint>();
        services.AddScoped<IAllocationExportModelBuilder, AllocationExportModelBuilder>();
    }
}