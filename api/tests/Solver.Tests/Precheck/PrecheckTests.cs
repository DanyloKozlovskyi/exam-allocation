using FluentAssertions;
using Solver.Model;
using Solver.Resources;
using Solver.Tests.Pipeline.Fakes;
using Xunit;

namespace Solver.Tests.Precheck;

public class PrecheckTests
{
    private static readonly SeparateByAttributeParameters SeparateOnePerRoom = new(TestInputBuilder.GroupKey, RuleScope.Room, 1);

    [Fact]
    public void ShouldReturnNoDiagnosticsForFeasibleInput()
    {
        // Arrange
        var input = new TestInputBuilder()
            .WithRoom(2, 2, 2).WithRoom(2, 2, 2)
            .WithGroup("A", 2).WithGroup("B", 2)
            .WithRule(RuleType.SeparateByAttribute, SeparateOnePerRoom)
            .Build();

        // Act
        var diagnostics = Run(input);

        // Assert
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReportNoActiveRooms()
    {
        // Arrange
        var input = new TestInputBuilder().WithGroup("A", 1).Build();

        // Act
        var diagnostics = Run(input);

        // Assert
        Single(diagnostics, DiagnosticCode.NoActiveRooms).Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public void ShouldReportCapacityTotalExceeded()
    {
        // Arrange
        var input = new TestInputBuilder().WithRoom(1, 2, 1).WithGroup("A", 3).Build();

        // Act
        var diagnostic = Single(Run(input), DiagnosticCode.CapacityTotalExceeded);

        // Assert
        diagnostic.Arguments[DiagnosticArgument.Size].Should().Be("3");
        diagnostic.Arguments[DiagnosticArgument.Limit].Should().Be("2");
    }

    [Fact]
    public void ShouldReportAccessibleCapacityExceeded()
    {
        // Arrange
        var input = new TestInputBuilder()
            .WithRoom(1, 4, 1)
            .WithGroup("A", 2, needsAccessibleSeat: true)
            .Build()
            .WithAccessibleSeats(0, "r0c0s0");

        // Act
        var diagnostic = Single(Run(input), DiagnosticCode.AccessibleCapacityExceeded);

        // Assert
        diagnostic.Arguments[DiagnosticArgument.Size].Should().Be("2");
        diagnostic.Arguments[DiagnosticArgument.Limit].Should().Be("1");
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void ShouldReportSeparationImpossibleWhenGroupExceedsScopeUnits(int rooms, bool expected)
    {
        // Arrange — docs/08: 3 groups × 4 people, maxPerScope = 1.
        var builder = new TestInputBuilder();
        for (var i = 0; i < rooms; i++)
        {
            builder.WithRoom(1, 5, 1);
        }

        var input = builder.WithGroup("A", 4).WithGroup("B", 4).WithGroup("C", 4).WithRule(RuleType.SeparateByAttribute, SeparateOnePerRoom).Build();

        // Act
        var diagnostics = Run(input).Where(d => d.Code == DiagnosticCode.SeparationImpossible).ToList();

        // Assert
        if (!expected)
        {
            diagnostics.Should().BeEmpty();
            return;
        }

        diagnostics.Should().HaveCount(3);
        diagnostics.Should().OnlyContain(d => d.RuleId == input.Rules[0].RuleId
            && d.Arguments[DiagnosticArgument.Size] == "4"
            && d.Arguments[DiagnosticArgument.Limit] == "3"
            && d.Arguments[DiagnosticArgument.AttributeKey] == TestInputBuilder.GroupKey);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void ShouldReportNoAdjacencyImpossibleFromIndependentSets(int rooms, bool expected)
    {
        // Arrange — docs/08: 1-row room of 4 single-seat desks (α = 2), one group of 3.
        var builder = new TestInputBuilder();
        for (var i = 0; i < rooms; i++)
        {
            builder.WithRoom(1, 4, 1);
        }

        var input = builder
            .WithGroup("A", 3)
            .WithRule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(TestInputBuilder.GroupKey, [AdjacencyKind.Side], false))
            .Build();

        // Act
        var diagnostics = Run(input).Where(d => d.Code == DiagnosticCode.NoAdjacencyImpossible).ToList();

        // Assert
        if (!expected)
        {
            diagnostics.Should().BeEmpty();
            return;
        }

        var diagnostic = diagnostics.Should().ContainSingle().Subject;
        diagnostic.Arguments[DiagnosticArgument.Value].Should().Be("A");
        diagnostic.Arguments[DiagnosticArgument.Size].Should().Be("3");
        diagnostic.Arguments[DiagnosticArgument.Limit].Should().Be("2");
    }

    [Fact]
    public void ShouldReportPinInvalidSeatForMissingOrDisabledSeat()
    {
        // Arrange
        var builder = new TestInputBuilder().WithRoom(1, 2, 1).WithGroup("A", 2);
        var draft = builder.Build();
        var input = builder
            .WithPin(draft.Participants[0].ParticipantId, draft.Rooms[0].RoomId, SeatKeyFormat.Format(5, 5, 0))
            .WithPin(draft.Participants[1].ParticipantId, Guid.NewGuid())
            .Build();

        // Act
        var diagnostics = Run(input).Where(d => d.Code == DiagnosticCode.PinInvalidSeat).ToList();

        // Assert
        diagnostics.Should().HaveCount(2);
        diagnostics.Should().Contain(d => d.Arguments[DiagnosticArgument.SeatKey] == SeatKeyFormat.Format(5, 5, 0)
            && d.Arguments[DiagnosticArgument.ParticipantId] == draft.Participants[0].ParticipantId.ToString());
    }

    [Fact]
    public void ShouldReportPinDuplicateSeat()
    {
        // Arrange
        var builder = new TestInputBuilder().WithRoom(1, 2, 1).WithGroup("A", 2);
        var draft = builder.Build();
        var seat = SeatKeyFormat.Format(0, 1, 0);
        var input = builder
            .WithPin(draft.Participants[0].ParticipantId, draft.Rooms[0].RoomId, seat)
            .WithPin(draft.Participants[1].ParticipantId, draft.Rooms[0].RoomId, seat)
            .Build();

        // Act
        var diagnostic = Single(Run(input), DiagnosticCode.PinDuplicateSeat);

        // Assert
        diagnostic.Arguments[DiagnosticArgument.SeatKey].Should().Be(seat);
        diagnostic.Arguments[DiagnosticArgument.Count].Should().Be("2");
    }

    [Fact]
    public void ShouldReportPinRoomOverfull()
    {
        // Arrange
        var builder = new TestInputBuilder().WithRoom(1, 2, 1).WithRoom(1, 4, 1).WithGroup("A", 3);
        var draft = builder.Build();
        foreach (var participant in draft.Participants)
        {
            builder.WithPin(participant.ParticipantId, draft.Rooms[0].RoomId);
        }

        // Act
        var diagnostic = Single(Run(builder.Build()), DiagnosticCode.PinRoomOverfull);

        // Assert
        diagnostic.Arguments[DiagnosticArgument.RoomId].Should().Be(draft.Rooms[0].RoomId.ToString());
        diagnostic.Arguments[DiagnosticArgument.Size].Should().Be("3");
        diagnostic.Arguments[DiagnosticArgument.Limit].Should().Be("2");
    }

    [Fact]
    public void ShouldReportPinRuleConflictForSeparation()
    {
        // Arrange
        var builder = new TestInputBuilder().WithRoom(1, 4, 1).WithRoom(1, 4, 1).WithGroup("A", 2).WithRule(RuleType.SeparateByAttribute, SeparateOnePerRoom);
        var draft = builder.Build();
        builder.WithPin(draft.Participants[0].ParticipantId, draft.Rooms[0].RoomId).WithPin(draft.Participants[1].ParticipantId, draft.Rooms[0].RoomId);

        // Act
        var diagnostic = Single(Run(builder.Build()), DiagnosticCode.PinRuleConflict);

        // Assert
        diagnostic.RuleId.Should().Be(draft.Rules[0].RuleId);
        diagnostic.Arguments[DiagnosticArgument.Value].Should().Be("A");
        diagnostic.Arguments[DiagnosticArgument.Size].Should().Be("2");
    }

    [Fact]
    public void ShouldReportPinRuleConflictForAdjacentPinnedSeats()
    {
        // Arrange
        var builder = new TestInputBuilder()
            .WithRoom(1, 4, 1)
            .WithGroup("A", 2)
            .WithRule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(TestInputBuilder.GroupKey, [AdjacencyKind.Side], false));
        var draft = builder.Build();
        builder
            .WithPin(draft.Participants[0].ParticipantId, draft.Rooms[0].RoomId, SeatKeyFormat.Format(0, 0, 0))
            .WithPin(draft.Participants[1].ParticipantId, draft.Rooms[0].RoomId, SeatKeyFormat.Format(0, 1, 0));

        // Act
        var diagnostic = Single(Run(builder.Build()), DiagnosticCode.PinRuleConflict);

        // Assert
        diagnostic.RuleId.Should().Be(draft.Rules[0].RuleId);
        diagnostic.Arguments[DiagnosticArgument.RoomId].Should().Be(draft.Rooms[0].RoomId.ToString());
    }

    [Fact]
    public void ShouldWarnAboutMissingAttributes()
    {
        // Arrange
        var input = new TestInputBuilder().WithRoom(2, 2, 2).WithRoom(2, 2, 2).WithGroup("A", 2).WithRule(RuleType.SeparateByAttribute, SeparateOnePerRoom).Build();
        input = input with { Participants = [.. input.Participants, new SolverParticipant(Guid.NewGuid(), new Dictionary<string, string>(), false, "Z")] };

        // Act
        var diagnostics = Run(input);

        // Assert
        var diagnostic = diagnostics.Should().ContainSingle().Subject;
        diagnostic.Code.Should().Be(DiagnosticCode.AttributeMissing);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.Arguments[DiagnosticArgument.AttributeKey].Should().Be(TestInputBuilder.GroupKey);
        diagnostic.Arguments[DiagnosticArgument.Count].Should().Be("1");
    }

    [Fact]
    public void ShouldRenderMessagesWithAllPlaceholdersFilled()
    {
        // Arrange
        var input = new TestInputBuilder().WithRoom(1, 2, 1).WithGroup("A", 3).Build();

        // Act
        var message = DiagnosticMessages.Render(Single(Run(input), DiagnosticCode.CapacityTotalExceeded));

        // Assert
        message.Should().Contain("3").And.Contain("2").And.NotContain("{");
    }

    private static IReadOnlyList<Diagnostic> Run(SolverInput input) => TestSolver.Precheck().Run(input, CancellationToken.None);

    private static Diagnostic Single(IReadOnlyList<Diagnostic> diagnostics, DiagnosticCode code)
        => diagnostics.Should().ContainSingle(d => d.Code == code).Subject;
}