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
        using var provider = BuildProvider();
        var responseType = requestType.GetInterfaces()
            .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
            .GetGenericArguments()[0];
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);

        // Act
        var handler = provider.GetService(handlerType);

        // Assert
        handler.Should().NotBeNull();
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

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();
        return services.BuildServiceProvider();
    }
}