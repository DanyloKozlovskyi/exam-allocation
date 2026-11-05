using System.Text.Json.Serialization;
using Api.Constants;
using Api.Hubs;
using Api.Modules.Swagger;
using Api.Services.Abstract;
using Api.Services.Implementation;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Microsoft.OpenApi;

namespace Api.Modules;

internal static class SetupModule
{
    internal static void SetupServices(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = services.SetupConfiguration(configuration);
        services.SetupControllers();
        services.SetupSignalR();
        services.SetupCors(settings);
        services.SetupSwagger();
        services.SetupControllerServices();
        services.SetupExceptionHandling();
    }

    private static ApplicationSettings SetupConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.Get<ApplicationSettings>() ?? new ApplicationSettings();
        services.AddSingleton(settings);
        return settings;
    }

    private static void SetupControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    }

    private static void SetupSignalR(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddSingleton<ISolveProgressNotifier, SignalRSolveProgressNotifier>();
    }

    private static void SetupCors(this IServiceCollection services, ApplicationSettings settings)
    {
        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins(settings.Cors.Origins.ToArray())
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()));
    }

    private static void SetupSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(SwaggerDocument.Name, new OpenApiInfo { Title = SwaggerDocument.Title, Version = SwaggerDocument.Name });
            options.UseOneOfForPolymorphism();
            options.UseAllOfForInheritance();
            options.SelectSubTypesUsing(SwaggerPolymorphism.SubTypes);
            options.SelectDiscriminatorNameUsing(SwaggerPolymorphism.DiscriminatorName);
            options.SelectDiscriminatorValueUsing(SwaggerPolymorphism.DiscriminatorValue);
            options.SupportNonNullableReferenceTypes();
            options.DocumentFilter<ContractSchemasDocumentFilter>();
        });
    }

    private static void SetupControllerServices(this IServiceCollection services)
    {
        services.AddScoped<ISessionsControllerService, SessionsControllerService>();
        services.AddScoped<ITestCentersControllerService, TestCentersControllerService>();
        services.AddScoped<IRoomsControllerService, RoomsControllerService>();
        services.AddScoped<IParticipantsControllerService, ParticipantsControllerService>();
        services.AddScoped<IRulesControllerService, RulesControllerService>();
        services.AddScoped<IPinsControllerService, PinsControllerService>();
        services.AddScoped<IImportsControllerService, ImportsControllerService>();
        services.AddScoped<ISolveRunsControllerService, SolveRunsControllerService>();
        services.AddScoped<IAllocationsControllerService, AllocationsControllerService>();
        services.AddScoped<IExportsControllerService, ExportsControllerService>();
    }
}