using Microsoft.Extensions.DependencyInjection;

namespace Tests.Common;

public static class TestFactoryExtensions
{
    public static void RemoveServiceByType(this IServiceCollection services, Type serviceType)
    {
        var descriptors = services.Where(s => s.ServiceType == serviceType).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }
}