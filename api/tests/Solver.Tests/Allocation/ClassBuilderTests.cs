using FluentAssertions;
using Solver.Allocation;
using Solver.Model;
using Xunit;

namespace Solver.Tests.Allocation;

public class ClassBuilderTests
{
    private const string CityKey = "city";

    private readonly ClassBuilder builder = new();

    [Fact]
    public void ShouldMergeParticipantsThatDifferOnlyInUnusedAttributes()
    {
        // Arrange
        var input = new TestInputBuilder()
            .WithRoom(2, 2, 2)
            .WithGroup("A", 4)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 1))
            .Build();
        input = input with
        {
            Participants = input.Participants
                .Select((p, i) => p with { Attributes = new Dictionary<string, string>(p.Attributes) { [CityKey] = "city" + i } })
                .ToList(),
        };

        // Act
        var classes = builder.Build(input);

        // Assert
        classes.Should().ContainSingle();
        classes[0].Size.Should().Be(4);
        classes[0].Values.Should().ContainKey(TestInputBuilder.GroupKey).And.NotContainKey(CityKey);
    }

    [Fact]
    public void ShouldSplitByReferencedAttributeAndAccessibility()
    {
        // Arrange
        var input = new TestInputBuilder()
            .WithRoom(2, 2, 2)
            .WithGroup("A", 3)
            .WithGroup("B", 2)
            .WithGroup("A", 1, needsAccessibleSeat: true)
            .WithRule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(TestInputBuilder.GroupKey, [AdjacencyKind.Side], false))
            .Build();

        // Act
        var classes = builder.Build(input);

        // Assert
        classes.Should().HaveCount(3);
        classes.Sum(c => c.Size).Should().Be(6);
        classes.Select(c => c.Index).Should().Equal(Enumerable.Range(0, 3));
        classes.Should().ContainSingle(c => c.NeedsAccessibleSeat && c.Size == 1);
        classes.Should().ContainSingle(c => !c.NeedsAccessibleSeat && c.Values[TestInputBuilder.GroupKey] == "A" && c.Size == 3);
    }

    [Fact]
    public void ShouldMergeEveryoneWhenNoRuleReferencesAttributes()
    {
        // Arrange
        var input = new TestInputBuilder().WithRoom(2, 2, 2).WithGroup("A", 2).WithGroup("B", 3).Build();

        // Act
        var classes = builder.Build(input);

        // Assert
        classes.Should().ContainSingle().Which.Size.Should().Be(5);
        classes[0].Values.Should().BeEmpty();
    }

    [Fact]
    public void ShouldGiveParticipantsWithMissingValueTheirOwnClass()
    {
        // Arrange
        var input = new TestInputBuilder()
            .WithRoom(2, 2, 2)
            .WithGroup("A", 2)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 1))
            .Build();
        var missing = Enumerable.Range(0, 3)
            .Select(i => new SolverParticipant(Guid.NewGuid(), i == 0 ? new Dictionary<string, string>() : new Dictionary<string, string> { [TestInputBuilder.GroupKey] = " " }, false, "x" + i))
            .ToList();
        input = input with { Participants = [.. input.Participants, .. missing] };

        // Act
        var classes = builder.Build(input);

        // Assert
        classes.Should().HaveCount(4);
        classes.Where(c => !c.Values.ContainsKey(TestInputBuilder.GroupKey)).Should().HaveCount(3).And.OnlyContain(c => c.Size == 1);
    }

    [Fact]
    public void ShouldExcludePinnedParticipants()
    {
        // Arrange
        var builderInput = new TestInputBuilder().WithRoom(2, 2, 2).WithGroup("A", 4);
        var draft = builderInput.Build();
        var input = builderInput.WithPin(draft.Participants[0].ParticipantId, draft.Rooms[0].RoomId).Build();

        // Act
        var classes = builder.Build(input);

        // Assert
        classes.SelectMany(c => c.ParticipantIds).Should().HaveCount(3).And.NotContain(draft.Participants[0].ParticipantId);
    }
}