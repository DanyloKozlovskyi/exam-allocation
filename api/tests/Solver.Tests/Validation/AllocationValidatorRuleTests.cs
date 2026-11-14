using FluentAssertions;
using Solver.Model;
using Xunit;
using static Solver.Tests.Validation.ValidatorScenario;

namespace Solver.Tests.Validation;

public class AllocationValidatorRuleTests
{
    private const int Weight = 7;

    [Fact]
    public void ShouldReportHardSeparationPerParticipantOverRoomLimit()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 6, 1);
        var rule = scenario.Rule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(GroupKey, RuleScope.Room, 1));
        scenario.Seated("A", room, Seat(0, 0));
        scenario.Seated("A", room, Seat(0, 2));
        scenario.Seated("A", room, Seat(0, 4));
        scenario.Seated("B", room, Seat(0, 5));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeFalse();
        report.Violations.Should().HaveCount(2)
            .And.OnlyContain(x => x.Kind == ViolationKind.SeparationViolated && x.IsHard && x.RuleId == rule);
        report.SoftPenalty.Should().Be(0);
    }

    [Fact]
    public void ShouldAcceptSeparationWhenGroupIsSplitAcrossRooms()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var first = scenario.Room(1, 2, 1);
        var second = scenario.Room(1, 2, 1);
        scenario.Rule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(GroupKey, RuleScope.Room, 1));
        scenario.Seated("A", first, Seat(0, 0));
        scenario.Seated("A", second, Seat(0, 0));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldCountSeparationPerCenter()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var otherCenter = Guid.NewGuid();
        var first = scenario.Room(1, 2, 1);
        var second = scenario.Room(1, 2, 1);
        var third = scenario.Room(1, 2, 1, centerId: otherCenter);
        scenario.Rule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(GroupKey, RuleScope.Center, 1));
        scenario.Seated("A", first, Seat(0, 0));
        scenario.Seated("A", second, Seat(0, 0));
        scenario.Seated("A", third, Seat(0, 0));

        // Act
        var report = scenario.Validate();

        // Assert
        report.Violations.Should().ContainSingle()
            .Which.Kind.Should().Be(ViolationKind.SeparationViolated);
    }

    [Fact]
    public void ShouldPenalizeSoftSeparationByWeightTimesExcess()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 6, 1);
        var rule = scenario.Rule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(GroupKey, RuleScope.Room, 1), isHard: false, weight: Weight);
        scenario.Seated("A", room, Seat(0, 0));
        scenario.Seated("A", room, Seat(0, 2));
        scenario.Seated("A", room, Seat(0, 4));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeTrue();
        report.Violations.Should().HaveCount(2).And.OnlyContain(x => !x.IsHard);
        report.PenaltyByRule[rule].Should().Be(2 * Weight);
        report.SoftPenalty.Should().Be(2 * Weight);
    }

    [Fact]
    public void ShouldTreatMissingAttributeValuesAsUnique()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 3, 2);
        scenario.Rule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(GroupKey, RuleScope.Room, 1));
        scenario.Rule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(GroupKey, [AdjacencyKind.Side], false));
        scenario.Rule(RuleType.KeepTogetherInCenter, new KeepTogetherInCenterParameters(GroupKey), isHard: false);
        scenario.Seated(null, room, Seat(0, 0, 0));
        scenario.Seated(null, room, Seat(0, 0, 1));
        scenario.Seated(null, room, Seat(0, 1, 0));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeTrue();
        report.Violations.Should().BeEmpty();
    }

    [Fact]
    public void ShouldAlwaysTreatSameDeskAsAdjacent()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 1, 2);
        var rule = scenario.Rule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(GroupKey, [], false));
        scenario.Seated("A", room, Seat(0, 0, 0));
        scenario.Seated("A", room, Seat(0, 0, 1));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeFalse();
        report.Violations.Should().ContainSingle()
            .Which.Should().Match<Violation>(x => x.Kind == ViolationKind.AdjacencyViolated && x.IsHard && x.RuleId == rule);
    }

    [Theory]
    [InlineData(AdjacencyKind.Side, 0, 0, 0, 1, true)]
    [InlineData(AdjacencyKind.FrontBack, 0, 0, 0, 1, false)]
    [InlineData(AdjacencyKind.Side, 0, 1, 0, 2, false)]
    [InlineData(AdjacencyKind.SideAcrossAisle, 0, 1, 0, 2, true)]
    [InlineData(AdjacencyKind.SideAcrossAisle, 0, 0, 0, 1, false)]
    [InlineData(AdjacencyKind.FrontBack, 0, 0, 1, 0, true)]
    [InlineData(AdjacencyKind.Side, 0, 0, 1, 0, false)]
    [InlineData(AdjacencyKind.Diagonal, 0, 0, 1, 1, true)]
    [InlineData(AdjacencyKind.Diagonal, 0, 1, 1, 2, false)]
    [InlineData(AdjacencyKind.Diagonal, 0, 0, 2, 2, false)]
    public void ShouldDetectAdjacencyPerKind(AdjacencyKind kind, int row1, int column1, int row2, int column2, bool violated)
    {
        // Arrange: 3 rows × 4 single-seat desks, aisle between columns 1 and 2.
        var scenario = new ValidatorScenario();
        var room = scenario.Room(3, 4, 1, aisles: [1]);
        scenario.Rule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(GroupKey, [kind], false));
        scenario.Seated("A", room, Seat(row1, column1));
        scenario.Seated("A", room, Seat(row2, column2));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().Be(!violated);
    }

    [Theory]
    [InlineData(1, 0, true)]
    [InlineData(0, 0, false)]
    [InlineData(1, 1, false)]
    public void ShouldApplySeatLevelSide(int leftSeat, int rightSeat, bool violated)
    {
        // Arrange: two 2-seat desks next to each other.
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 2, 2);
        scenario.Rule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(GroupKey, [AdjacencyKind.Side], true));
        scenario.Seated("A", room, Seat(0, 0, leftSeat));
        scenario.Seated("A", room, Seat(0, 1, rightSeat));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().Be(!violated);
    }

    [Fact]
    public void ShouldIgnoreAdjacencyBetweenDifferentValues()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 3, 2);
        scenario.Rule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(GroupKey, [AdjacencyKind.Side], false));
        scenario.Seated("A", room, Seat(0, 0, 0));
        scenario.Seated("B", room, Seat(0, 0, 1));
        scenario.Seated("C", room, Seat(0, 1, 0));
        scenario.Seated("A", room, Seat(0, 2, 1));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldPenalizeSoftAdjacencyPerViolatingPair()
    {
        // Arrange: A A A in a row of single seats → 2 side pairs.
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 3, 1);
        var rule = scenario.Rule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(GroupKey, [AdjacencyKind.Side], false), isHard: false, weight: Weight);
        scenario.Seated("A", room, Seat(0, 0));
        scenario.Seated("A", room, Seat(0, 1));
        scenario.Seated("A", room, Seat(0, 2));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeTrue();
        report.Violations.Should().HaveCount(2).And.OnlyContain(x => x.Kind == ViolationKind.AdjacencyViolated && !x.IsHard);
        report.PenaltyByRule[rule].Should().Be(2 * Weight);
    }

    [Fact]
    public void ShouldPenalizeMinRoomFillShortfallOfUsedRoomsOnly()
    {
        // Arrange: capacity 8, ratio 0.6 → ⌈4.8⌉ = 5 required, 3 seated → shortfall 2; second room unused.
        var scenario = new ValidatorScenario();
        var room = scenario.Room(2, 2, 2);
        scenario.Room(2, 2, 2);
        var rule = scenario.Rule(RuleType.MinRoomFill, new MinRoomFillParameters(0.6), isHard: false, weight: Weight);
        scenario.Seated("A", room, Seat(0, 0, 0));
        scenario.Seated("B", room, Seat(0, 1, 0));
        scenario.Seated("C", room, Seat(1, 0, 0));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeTrue();
        report.Violations.Should().ContainSingle()
            .Which.Should().Match<Violation>(x => x.Kind == ViolationKind.MinRoomFillShortfall && x.RoomId == room);
        report.PenaltyByRule[rule].Should().Be(2 * Weight);
    }

    [Fact]
    public void ShouldNotRoundUpExactFillRequirement()
    {
        // Arrange: 0.7 · 10 is 7.000000000000001 in binary floating point; 7 seated must be enough.
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 10, 1);
        var rule = scenario.Rule(RuleType.MinRoomFill, new MinRoomFillParameters(0.7), isHard: false, weight: Weight);
        for (var column = 0; column < 7; column++)
        {
            scenario.Seated($"G{column}", room, Seat(0, column));
        }

        // Act
        var report = scenario.Validate();

        // Assert
        report.PenaltyByRule[rule].Should().Be(0);
    }

    [Fact]
    public void ShouldPenalizeExtraCentersForKeepTogether()
    {
        // Arrange: A in 3 centers → 2 extra; B in 1 center (2 rooms) → 0.
        var scenario = new ValidatorScenario();
        var first = scenario.Room(1, 4, 1);
        var sameCenter = scenario.Room(1, 4, 1);
        var second = scenario.Room(1, 4, 1, centerId: Guid.NewGuid());
        var third = scenario.Room(1, 4, 1, centerId: Guid.NewGuid());
        var rule = scenario.Rule(RuleType.KeepTogetherInCenter, new KeepTogetherInCenterParameters(GroupKey), isHard: false, weight: Weight);
        scenario.Seated("A", first, Seat(0, 0));
        scenario.Seated("A", first, Seat(0, 2));
        scenario.Seated("A", second, Seat(0, 0));
        scenario.Seated("A", third, Seat(0, 0));
        scenario.Seated("B", first, Seat(0, 3));
        scenario.Seated("B", sameCenter, Seat(0, 3));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeTrue();
        report.Violations.Should().HaveCount(2).And.OnlyContain(x => x.Kind == ViolationKind.KeepTogetherSplit);
        report.PenaltyByRule[rule].Should().Be(2 * Weight);
    }

    [Fact]
    public void ShouldPenalizeOccupiedAdjacentPairsForSpread()
    {
        // Arrange: 1 row × 3 desks × 2 seats; occupied c0s0, c0s1 (same desk), c1s0 (side to both), c2s1 (side to c1s0).
        var scenario = new ValidatorScenario();
        var room = scenario.Room(1, 3, 2);
        var rule = scenario.Rule(RuleType.SpreadInRoom, new SpreadInRoomParameters([AdjacencyKind.Side]), isHard: false, weight: Weight);
        scenario.Seated("A", room, Seat(0, 0, 0));
        scenario.Seated("B", room, Seat(0, 0, 1));
        scenario.Seated("C", room, Seat(0, 1, 0));
        scenario.Seated("D", room, Seat(0, 2, 1));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeTrue();
        report.Violations.Should().HaveCount(4).And.OnlyContain(x => x.Kind == ViolationKind.SpreadViolated);
        report.PenaltyByRule[rule].Should().Be(4 * Weight);
    }

    [Fact]
    public void ShouldPenalizeRowIndexSumForFillFromFront()
    {
        // Arrange: rows 0, 1, 2 occupied → Σ = 3.
        var scenario = new ValidatorScenario();
        var room = scenario.Room(3, 1, 1);
        var rule = scenario.Rule(RuleType.FillFromFront, new FillFromFrontParameters(), isHard: false, weight: Weight);
        scenario.Seated("A", room, Seat(0, 0));
        scenario.Seated("B", room, Seat(1, 0));
        scenario.Seated("C", room, Seat(2, 0));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeTrue();
        report.Violations.Should().ContainSingle().Which.Kind.Should().Be(ViolationKind.FillFromFront);
        report.PenaltyByRule[rule].Should().Be(3 * Weight);
    }

    [Fact]
    public void ShouldSumSoftPenaltiesAndListOnlySoftRules()
    {
        // Arrange
        var scenario = new ValidatorScenario();
        var room = scenario.Room(2, 1, 1);
        var hard = scenario.Rule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(GroupKey, RuleScope.Room, 2));
        var front = scenario.Rule(RuleType.FillFromFront, new FillFromFrontParameters(), isHard: false, weight: Weight);
        var spread = scenario.Rule(RuleType.SpreadInRoom, new SpreadInRoomParameters([AdjacencyKind.FrontBack]), isHard: false, weight: 1);
        scenario.Seated("A", room, Seat(0, 0));
        scenario.Seated("A", room, Seat(1, 0));

        // Act
        var report = scenario.Validate();

        // Assert
        report.IsValid.Should().BeTrue();
        report.PenaltyByRule.Should().NotContainKey(hard);
        report.PenaltyByRule.Should().Contain(front, Weight).And.Contain(spread, 1);
        report.SoftPenalty.Should().Be(Weight + 1);
    }
}