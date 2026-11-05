using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Solver.Model;
using Solver.Pipeline;
using Solver.Resources;
using Xunit;

namespace Solver.Tests.Foundation;

public class FoundationTests
{
    [Fact]
    public void ShouldBuildInputWithBuilder()
    {
        // Act
        var input = new TestInputBuilder()
            .WithRoom(rows: 2, columns: 2, seatsPerDesk: 2)
            .WithGroup("A", 3)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 1))
            .Build();

        // Assert
        input.Rooms.Should().HaveCount(1);
        input.Participants.Should().HaveCount(3);
        input.Rules.Should().ContainSingle();
    }

    [Fact]
    public void ShouldResolveAllSolverServices()
    {
        // Arrange
        var provider = new ServiceCollection().AddSolver().BuildServiceProvider();

        // Act
        var pipeline = provider.GetService<ISolverPipeline>();

        // Assert
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void ShouldFormatSeatKeys()
    {
        SeatKeyFormat.Format(1, 2, 0).Should().Be("r1c2s0");
    }

    [Fact]
    public void ShouldHaveMessageForEveryDiagnosticCode()
    {
        foreach (var code in Enum.GetValues<DiagnosticCode>())
        {
            var message = DiagnosticMessages.Render(new Diagnostic(code, DiagnosticSeverity.Error, null, new Dictionary<DiagnosticArgument, string>()));
            message.Should().NotBe(code.ToString(), $"resource for {code} must exist");
        }
    }
}