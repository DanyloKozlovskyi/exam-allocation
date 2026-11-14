using System.Text.Json.Serialization;
using Domain.ValueObjects;

namespace Domain.Rules.Parameters;

/// <summary>Typed rule parameters, polymorphic JSON on the "type" discriminator.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = RuleParameters.TypeDiscriminator)]
[JsonDerivedType(typeof(SeparateByAttributeParameters), nameof(RuleType.SeparateByAttribute))]
[JsonDerivedType(typeof(NoAdjacentByAttributeParameters), nameof(RuleType.NoAdjacentByAttribute))]
[JsonDerivedType(typeof(AccessibleSeatingParameters), nameof(RuleType.AccessibleSeating))]
[JsonDerivedType(typeof(MinRoomFillParameters), nameof(RuleType.MinRoomFill))]
[JsonDerivedType(typeof(KeepTogetherInCenterParameters), nameof(RuleType.KeepTogetherInCenter))]
[JsonDerivedType(typeof(SpreadInRoomParameters), nameof(RuleType.SpreadInRoom))]
[JsonDerivedType(typeof(FillFromFrontParameters), nameof(RuleType.FillFromFront))]
public abstract record RuleParameters
{
    public const string TypeDiscriminator = "type";

    [JsonIgnore]
    public abstract RuleType RuleType { get; }

    /// <summary>Default parameters offered by the rule editor for <paramref name="type"/>.</summary>
    public static RuleParameters CreateDefault(RuleType type) => type switch
    {
        RuleType.SeparateByAttribute => new SeparateByAttributeParameters(AttributeKeys.Group, RuleScope.Room, RuleConstraints.DefaultMaxPerScope),
        RuleType.NoAdjacentByAttribute => new NoAdjacentByAttributeParameters(
            AttributeKeys.Group,
            [AdjacencyKind.SameDesk, AdjacencyKind.Side, AdjacencyKind.FrontBack],
            false),
        RuleType.AccessibleSeating => new AccessibleSeatingParameters(),
        RuleType.MinRoomFill => new MinRoomFillParameters(RuleConstraints.DefaultMinFillRatio),
        RuleType.KeepTogetherInCenter => new KeepTogetherInCenterParameters(AttributeKeys.School),
        RuleType.SpreadInRoom => new SpreadInRoomParameters([AdjacencyKind.Side]),
        RuleType.FillFromFront => new FillFromFrontParameters(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported rule type"),
    };
}

public sealed record SeparateByAttributeParameters(string AttributeKey, RuleScope Scope, int MaxPerScope) : RuleParameters
{
    public override RuleType RuleType => RuleType.SeparateByAttribute;
}

public sealed record NoAdjacentByAttributeParameters(string AttributeKey, IReadOnlyList<AdjacencyKind> Adjacency, bool SeatLevelSide) : RuleParameters
{
    public override RuleType RuleType => RuleType.NoAdjacentByAttribute;
}

public sealed record AccessibleSeatingParameters : RuleParameters
{
    public override RuleType RuleType => RuleType.AccessibleSeating;
}

public sealed record MinRoomFillParameters(double MinFillRatio) : RuleParameters
{
    public override RuleType RuleType => RuleType.MinRoomFill;
}

public sealed record KeepTogetherInCenterParameters(string AttributeKey) : RuleParameters
{
    public override RuleType RuleType => RuleType.KeepTogetherInCenter;
}

public sealed record SpreadInRoomParameters(IReadOnlyList<AdjacencyKind> Adjacency) : RuleParameters
{
    public override RuleType RuleType => RuleType.SpreadInRoom;
}

public sealed record FillFromFrontParameters : RuleParameters
{
    public override RuleType RuleType => RuleType.FillFromFront;
}