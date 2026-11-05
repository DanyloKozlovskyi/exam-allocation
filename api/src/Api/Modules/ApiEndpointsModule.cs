using Api.Constants;

namespace Api.Modules;

internal static class ApiEndpointsModule
{
    public const string HealthyResponse = "healthy";

    internal static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapGet(ApiRoutes.Health, () => HealthyResponse);
        return app;
    }
}