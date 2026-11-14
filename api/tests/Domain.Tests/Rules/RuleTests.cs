using System.Text.Json;
using Domain.Rules;
using Domain.Rules.Parameters;
using Domain.Sessions;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Rules;

public class RuleTests
{
    [Theory]
    [InlineData(RuleType.SeparateByAttribute, true, true)]
    [InlineData(RuleType.NoAdjacentByAttribute, true, true)]
    [InlineData(RuleType.AccessibleSeating, true, false)]
    [InlineData(RuleType.MinRoomFill, false, true)]
    [InlineData(RuleType.KeepTogetherInCenter, false, true)]
    [InlineData(RuleType.SpreadInRoom, false, true)]
    [InlineData(RuleType.FillFromFront, false, true)]
    public void ShouldExposeHardSoftPolicyPerType(RuleType type, bool allowsHard, bool allowsSoft)
    {
        // Assert
        RuleConstraints.AllowsHard(type).Should().Be(allowsHard);
        RuleConstraints.AllowsSoft(type).Should().Be(allowsSoft);
    }

    [Fact]
    public void ShouldCreateDefaultParametersForEveryType()
    {
        foreach (var type in Enum.GetValues<RuleType>())
        {
            // Act
            var parameters = RuleParameters.CreateDefault(type);

            // Assert
            parameters.RuleType.Should().Be(type);
        }
    }

    [Fact]
    public void ShouldRoundTripPolymorphicParametersThroughJson()
    {
        // Arrange
        RuleParameters parameters = new NoAdjacentByAttributeParameters(AttributeKeys.Group, [AdjacencyKind.SameDesk, AdjacencyKind.Side], true);

        // Act
        var json = JsonSerializer.Serialize(parameters);
        var restored = JsonSerializer.Deserialize<RuleParameters>(json);

        // Assert
        json.Should().Contain(nameof(RuleType.NoAdjacentByAttribute));
        restored.Should().BeOfType<NoAdjacentByAttributeParameters>()
            .Which.Adjacency.Should().Equal(AdjacencyKind.SameDesk, AdjacencyKind.Side);
    }

    [Fact]
    public void ShouldRejectModeNotAllowedByType()
    {
        // Act
        var softAccessible = () => Rule.New(RuleId.New(), ExamSessionId.New(), new AccessibleSeatingParameters(), false, RuleConstraints.DefaultWeight, true, null);
        var hardFill = () => Rule.New(RuleId.New(), ExamSessionId.New(), new FillFromFrontParameters(), true, RuleConstraints.DefaultWeight, true, null);

        // Assert
        softAccessible.Should().Throw<ArgumentException>();
        hardFill.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldUpdateTypeWithParameters()
    {
        // Arrange
        var rule = Rule.New(RuleId.New(), ExamSessionId.New(), new FillFromFrontParameters(), false, RuleConstraints.DefaultWeight, true, " ");

        // Act
        rule.UpdateDetails(new SeparateByAttributeParameters(AttributeKeys.School, RuleScope.Center, 2), true, RuleConstraints.MaxWeight, false, "Школи окремо");

        // Assert
        rule.Type.Should().Be(RuleType.SeparateByAttribute);
        rule.IsHard.Should().BeTrue();
        rule.IsEnabled.Should().BeFalse();
        rule.DisplayName.Should().Be("Школи окремо");
    }

    [Theory]
    [InlineData(RuleConstraints.MinWeight - 1)]
    [InlineData(RuleConstraints.MaxWeight + 1)]
    public void ShouldRejectWeightOutOfRange(int weight)
    {
        // Act
        var act = () => Rule.New(RuleId.New(), ExamSessionId.New(), new FillFromFrontParameters(), false, weight, true, null);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}