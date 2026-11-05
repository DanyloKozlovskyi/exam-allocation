namespace Solver.Model;

/// <summary>Solver-side rule parameters (primitive mirror of Domain rule parameters).</summary>
public abstract record RuleParameters;

public sealed record SeparateByAttributeParameters(string AttributeKey, RuleScope Scope, int MaxPerScope) : RuleParameters;

public sealed record NoAdjacentByAttributeParameters(string AttributeKey, IReadOnlyList<AdjacencyKind> Adjacency, bool SeatLevelSide) : RuleParameters;

public sealed record AccessibleSeatingParameters : RuleParameters;

public sealed record MinRoomFillParameters(double MinFillRatio) : RuleParameters;

public sealed record KeepTogetherInCenterParameters(string AttributeKey) : RuleParameters;

public sealed record SpreadInRoomParameters(IReadOnlyList<AdjacencyKind> Adjacency) : RuleParameters;

public sealed record FillFromFrontParameters : RuleParameters;