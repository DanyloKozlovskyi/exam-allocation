using Application;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.Tests.Architecture;

public class MediatRRegistrationTests
{
    public static TheoryData<Type> RequestTypes()
    {
        var data = new TheoryData<Type>();
        foreach (var type in RequestTypesInApplication())
        {
            data.Add(type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void ShouldResolveHandlerForEveryCommand(Type requestType)
    {
        // Arrange
        var services = BuildServices();
        var responseType = requestType.GetInterfaces()
            .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
            .GetGenericArguments()[0];
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);

        // Act
        var registration = services.SingleOrDefault(x => x.ServiceType == handlerType);

        // Assert (handlers depend on infrastructure services, so only the registration is checked)
        registration.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void ShouldHaveValidatorForEveryCommand(Type requestType)
    {
        // Arrange
        using var provider = BuildProvider();
        var validatorType = typeof(IValidator<>).MakeGenericType(requestType);

        // Act
        var validator = provider.GetService(validatorType);

        // Assert
        validator.Should().NotBeNull();
    }

    [Fact]
    public void ShouldFindCommands()
    {
        RequestTypesInApplication().Should().NotBeEmpty();
    }

    private static IEnumerable<Type> RequestTypesInApplication()
        => typeof(Application.ConfigureServices).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)));

    private static ServiceCollection BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();
        return services;
    }

    private static ServiceProvider BuildProvider() => BuildServices().BuildServiceProvider();
}