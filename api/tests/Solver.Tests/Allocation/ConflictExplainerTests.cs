using FluentAssertions;
using Solver.Model;
using Solver.Tests.Pipeline.Fakes;
using Xunit;

namespace Solver.Tests.Allocation;

public class ConflictExplainerTests
{
    [Fact]
    public void ShouldReturnMinimalCoreWithBothSeparationRules()
    {
        // Arrange
        var input = ConflictInput(out var roomRule, out var centerRule, out var unrelatedRule);

        // Act
        var diagnostics = TestSolver.Explainer().Explain(TestSolver.Request(input), CancellationToken.None);

        // Assert
        var core = diagnostics.Should().ContainSingle(d => d.Code == DiagnosticCode.ConflictCore).Subject;
        core.Severity.Should().Be(DiagnosticSeverity.Error);
        var ruleIds = core.Arguments[DiagnosticArgument.RuleIds].Split(',').Select(Guid.Parse).ToList();
        ruleIds.Should().BeEquivalentTo([roomRule, centerRule]);
        ruleIds.Should().NotContain(unrelatedRule);
    }

    [Fact]
    public void ShouldReturnNothingForFeasibleModel()
    {
        // Arrange
        var input = new TestInputBuilder()
            .WithRoom(1, 4, 1)
            .WithGroup("A", 2)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 2))
            .Build();

        // Act
        var diagnostics = TestSolver.Explainer().Explain(TestSolver.Request(input), CancellationToken.None);

        // Assert
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReportCenterCapacityInCore()
    {
        // Arrange — one center with 4 seats but a hard per-room cap of 4 in a single room; 5 people of value A.
        var input = new TestInputBuilder()
            .WithRoom(1, 4, 1)
            .WithRoom(1, 1, 1)
            .WithGroup("A", 5)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 3))
            .Build();

        // Act
        var diagnostics = TestSolver.Explainer().Explain(TestSolver.Request(input), CancellationToken.None);

        // Assert
        var core = diagnostics.Should().ContainSingle().Subject;
        core.Arguments.Should().ContainKey(DiagnosticArgument.CenterId);
        core.Arguments[DiagnosticArgument.RuleIds].Should().Contain(input.Rules[0].RuleId.ToString());
    }

    /// <summary>docs/08: R2 group/Room max 2 + R2 group/Center max 3; center A has 2 rooms, center B 1 room, group of 6.</summary>
    internal static SolverInput ConflictInput(out Guid roomRule, out Guid centerRule, out Guid unrelatedRule)
    {
        var centerA = Guid.NewGuid();
        var centerB = Guid.NewGuid();
        var input = new TestInputBuilder()
            .WithRoom(1, 4, 1, centerId: centerA)
            .WithRoom(1, 4, 1, centerId: centerA)
            .WithRoom(1, 4, 1, centerId: centerB)
            .WithGroup("A", 6)
            .WithGroup("B", 2)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 2))
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Center, 3))
            .WithRule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(TestInputBuilder.GroupKey, [AdjacencyKind.FrontBack], false))
            .Build();
        roomRule = input.Rules[0].RuleId;
        centerRule = input.Rules[1].RuleId;
        unrelatedRule = input.Rules[2].RuleId;
        return input;
    }
}