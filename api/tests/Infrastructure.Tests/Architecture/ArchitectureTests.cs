using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Infrastructure.Tests.Architecture;

public class ArchitectureTests
{
    private const string DomainNamespace = "Domain";
    private const string ApplicationNamespace = "Application";
    private const string InfrastructureNamespace = "Infrastructure";
    private const string ApiNamespace = "Api";
    private const string SolverNamespace = "Solver";

    [Fact]
    public void DomainShouldNotDependOnOtherLayers()
    {
        var result = Types.InAssembly(typeof(Domain.IEntity<>).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace, SolverNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void SolverShouldBePure()
    {
        var result = Types.InAssembly(typeof(Solver.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(DomainNamespace, ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void ApplicationShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(typeof(Application.ConfigureServices).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void InfrastructureShouldNotDependOnApi()
    {
        var result = Types.InAssembly(typeof(Infrastructure.ConfigureServices).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}