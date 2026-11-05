using System.Text.Json.Serialization;
using Application.Rules.Commands;
using Domain.Rules;
using Domain.Rules.Parameters;

namespace Api.Dtos;

[JsonPolymorphic(TypeDiscriminatorPropertyName = RuleParameters.TypeDiscriminator)]
[JsonDerivedType(typeof(SeparateByAttributeParametersDto), nameof(RuleType.SeparateByAttribute))]
[JsonDerivedType(typeof(NoAdjacentByAttributeParametersDto), nameof(RuleType.NoAdjacentByAttribute))]
[JsonDerivedType(typeof(AccessibleSeatingParametersDto), nameof(RuleType.AccessibleSeating))]
[JsonDerivedType(typeof(MinRoomFillParametersDto), nameof(RuleType.MinRoomFill))]
[JsonDerivedType(typeof(KeepTogetherInCenterParametersDto), nameof(RuleType.KeepTogetherInCenter))]
[JsonDerivedType(typeof(SpreadInRoomParametersDto), nameof(RuleType.SpreadInRoom))]
[JsonDerivedType(typeof(FillFromFrontParametersDto), nameof(RuleType.FillFromFront))]
public abstract record RuleParametersDto
{
    public static RuleParametersDto FromDomain(RuleParameters parameters) => parameters switch
    {
        SeparateByAttributeParameters p => new SeparateByAttributeParametersDto(p.AttributeKey, p.Scope, p.MaxPerScope),
        NoAdjacentByAttributeParameters p => new NoAdjacentByAttributeParametersDto(p.AttributeKey, p.Adjacency, p.SeatLevelSide),
        AccessibleSeatingParameters => new AccessibleSeatingParametersDto(),
        MinRoomFillParameters p => new MinRoomFillParametersDto(p.MinFillRatio),
        KeepTogetherInCenterParameters p => new KeepTogetherInCenterParametersDto(p.AttributeKey),
        SpreadInRoomParameters p => new SpreadInRoomParametersDto(p.Adjacency),
        FillFromFrontParameters => new FillFromFrontParametersDto(),
        _ => throw new ArgumentOutOfRangeException(nameof(parameters), parameters.GetType().Name, "Unsupported rule parameters"),
    };

    public abstract RuleParameters ToDomain();
}

public sealed record SeparateByAttributeParametersDto(string AttributeKey, RuleScope Scope, int MaxPerScope) : RuleParametersDto
{
    public override RuleParameters ToDomain() => new SeparateByAttributeParameters(AttributeKey, Scope, MaxPerScope);
}

public sealed record NoAdjacentByAttributeParametersDto(string AttributeKey, IReadOnlyList<AdjacencyKind> Adjacency, bool SeatLevelSide) : RuleParametersDto
{
    public override RuleParameters ToDomain() => new NoAdjacentByAttributeParameters(AttributeKey, Adjacency, SeatLevelSide);
}

public sealed record AccessibleSeatingParametersDto : RuleParametersDto
{
    public override RuleParameters ToDomain() => new AccessibleSeatingParameters();
}

public sealed record MinRoomFillParametersDto(double MinFillRatio) : RuleParametersDto
{
    public override RuleParameters ToDomain() => new MinRoomFillParameters(MinFillRatio);
}

public sealed record KeepTogetherInCenterParametersDto(string AttributeKey) : RuleParametersDto
{
    public override RuleParameters ToDomain() => new KeepTogetherInCenterParameters(AttributeKey);
}

public sealed record SpreadInRoomParametersDto(IReadOnlyList<AdjacencyKind> Adjacency) : RuleParametersDto
{
    public override RuleParameters ToDomain() => new SpreadInRoomParameters(Adjacency);
}

public sealed record FillFromFrontParametersDto : RuleParametersDto
{
    public override RuleParameters ToDomain() => new FillFromFrontParameters();
}

public record RuleDto(
    Guid? Id,
    Guid? SessionId,
    RuleType Type,
    bool IsHard,
    int Weight,
    bool IsEnabled,
    string? DisplayName,
    RuleParametersDto Parameters,
    uint? Version = null)
{
    public static RuleDto FromDomain(Rule rule) => new(
        rule.Id.Value,
        rule.SessionId.Value,
        rule.Type,
        rule.IsHard,
        rule.Weight,
        rule.IsEnabled,
        rule.DisplayName,
        RuleParametersDto.FromDomain(rule.Parameters),
        rule.Version);

    public CreateRuleCommand ToCreateCommand(Guid sessionId) => new()
    {
        SessionId = sessionId,
        Parameters = Parameters.ToDomain(),
        IsHard = IsHard,
        Weight = Weight,
        IsEnabled = IsEnabled,
        DisplayName = DisplayName,
    };

    public UpdateRuleCommand ToUpdateCommand(Guid sessionId) => new()
    {
        SessionId = sessionId,
        Id = Id ?? Guid.Empty,
        Parameters = Parameters.ToDomain(),
        IsHard = IsHard,
        Weight = Weight,
        IsEnabled = IsEnabled,
        DisplayName = DisplayName,
        Version = Version ?? default,
    };
}

public record RuleTypeInfoDto(RuleType Type, bool AllowsHard, bool AllowsSoft, RuleParametersDto DefaultParameters);