using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Bench;

/// <summary>
/// Tiny resolver over the descriptors registered by <c>AddSolver()</c>. Bench does not reference
/// Microsoft.Extensions.DependencyInjection (only the Abstractions come transitively from Solver), so it
/// cannot call <c>BuildServiceProvider()</c>; this keeps the real registrations and constructor injection.
/// </summary>
internal sealed class MinimalServiceProvider(IEnumerable<ServiceDescriptor> descriptors) : IServiceProvider
{
    private readonly IReadOnlyDictionary<Type, ServiceDescriptor> registrations = descriptors
        .GroupBy(x => x.ServiceType)
        .ToDictionary(x => x.Key, x => x.Last());

    private readonly ConcurrentDictionary<Type, object> singletons = new();

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider))
        {
            return this;
        }

        if (!registrations.TryGetValue(serviceType, out var descriptor))
        {
            return null;
        }

        return descriptor.Lifetime == ServiceLifetime.Singleton
            ? singletons.GetOrAdd(serviceType, _ => Create(descriptor))
            : Create(descriptor);
    }

    private object Create(ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is { } instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory is { } factory)
        {
            return factory(this);
        }

        var type = descriptor.ImplementationType ?? descriptor.ServiceType;
        return Construct(type);
    }

    /// <summary>
    /// Like Microsoft DI: use the public constructor with the most parameters that can all be resolved
    /// (or have defaults). <c>ActivatorUtilities</c> instead throws when several constructors qualify.
    /// </summary>
    private object Construct(Type type)
    {
        foreach (var constructor in type.GetConstructors().OrderByDescending(x => x.GetParameters().Length))
        {
            var parameters = constructor.GetParameters();
            var arguments = new object?[parameters.Length];
            var satisfiable = true;
            for (var i = 0; i < parameters.Length && satisfiable; i++)
            {
                var parameter = parameters[i];
                if (registrations.ContainsKey(parameter.ParameterType) || parameter.ParameterType == typeof(IServiceProvider))
                {
                    arguments[i] = GetService(parameter.ParameterType);
                }
                else if (parameter.HasDefaultValue)
                {
                    arguments[i] = parameter.DefaultValue;
                }
                else
                {
                    satisfiable = false;
                }
            }

            if (satisfiable)
            {
                return constructor.Invoke(arguments);
            }
        }

        throw new InvalidOperationException($"No public constructor of {type.FullName} can be satisfied from the registered services");
    }
}