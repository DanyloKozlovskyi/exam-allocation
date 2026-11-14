using FluentAssertions;
using Solver.Model;
using Solver.Tests.Pipeline;
using Solver.Tests.Pipeline.Fakes;
using Xunit;

namespace Solver.Tests.Alternatives;

public class AlternativesGeneratorTests
{
    [Fact]
    public async Task ShouldReturnDistinctValidAlternativesWithEnoughMovedParticipants()
    {
        // Arrange — docs/08: N = 3.
        var input = SolverPipelineTests.SmallInput() with { };
        input = input with { Options = input.Options with { Alternatives = 3, CostSlack = 0.5 } };
        var bestCostBound = 0m;

        // Act
        var output = await TestSolver.Pipeline().RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
        output.Allocations.Should().HaveCount(4);
        output.Allocations.Select(a => a.Index).Should().Equal(0, 1, 2, 3);
        bestCostBound = output.Allocations[0].Objective.Cost * 1.5m;
        var validator = new FakeValidator();
        output.Allocations.Should().OnlyContain(a => validator.Validate(input, a.Items).IsValid);
        output.Allocations.Should().OnlyContain(a => a.Objective.Cost <= bestCostBound);
        var minimumMoved = (int)Math.Ceiling(input.Options.DiversityRatio * input.Participants.Count);
        for (var i = 0; i < output.Allocations.Count; i++)
        {
            for (var j = i + 1; j < output.Allocations.Count; j++)
            {
                var a = Placement(output.Allocations[i]);
                var b = Placement(output.Allocations[j]);
                var movedRooms = a.Count(e => b[e.Key].RoomId != e.Value.RoomId);
                var seatDistinct = a.Any(e => b[e.Key] != e.Value);
                (movedRooms >= minimumMoved || seatDistinct).Should().BeTrue();
                seatDistinct.Should().BeTrue("alternatives must be distinct");
            }
        }
    }

    [Fact]
    public async Task ShouldFallBackToSeatLevelDiversityWhenRoomsAreForced()
    {
        // Arrange — one room exactly full: stage-1 diversity is impossible.
        var input = new TestInputBuilder()
            .WithRoom(2, 3, 1)
            .WithGroup("A", 3).WithGroup("B", 3)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 3))
            .WithOptions(o => o with { Alternatives = 2 })
            .Build();

        // Act
        var output = await TestSolver.Pipeline().RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Allocations.Should().HaveCount(3);
        output.Allocations.Should().OnlyContain(a => a.Items.All(i => i.RoomId == input.Rooms[0].RoomId));
        output.Allocations.Select(a => string.Join(";", a.Items.OrderBy(i => i.ParticipantId).Select(i => i.SeatKey))).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task ShouldReportAlternativesLimitedWhenNoDistinctAllocationExists()
    {
        // Arrange — a single participant on a single seat.
        var input = new TestInputBuilder().WithRoom(1, 1, 1).WithGroup("A", 1).WithOptions(o => o with { Alternatives = 2 }).Build();

        // Act
        var output = await TestSolver.Pipeline().RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Allocations.Should().ContainSingle();
        var diagnostic = output.Diagnostics.Should().ContainSingle(d => d.Code == DiagnosticCode.AlternativesLimited).Subject;
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.Arguments[DiagnosticArgument.Reason].Should().Be(nameof(Solver.Alternatives.AlternativesLimitReason.NoDistinctAllocation));
        Solver.Resources.DiagnosticMessages.Render(diagnostic).Should().NotContain("{").And.NotContain(nameof(Solver.Alternatives.AlternativesLimitReason.NoDistinctAllocation));
    }

    private static Dictionary<Guid, (Guid RoomId, string SeatKey)> Placement(SolvedAllocation allocation)
        => allocation.Items.ToDictionary(i => i.ParticipantId, i => (i.RoomId, i.SeatKey));
}