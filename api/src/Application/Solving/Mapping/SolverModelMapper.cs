using Domain.Allocations;
using Domain.Rooms;
using Domain.Rules;
using Domain.SolveRuns;
using Solver.Model;
using DomainParameters = Domain.Rules.Parameters;
using SolverDiagnostic = Solver.Model.Diagnostic;
using SolverParameters = Solver.Model;

namespace Application.Solving.Mapping;

/// <summary>Pure mapping functions between Domain types and the Solver model.</summary>
public static class SolverModelMapper
{
    public static RoomLayoutModel ToSolverLayout(RoomLayout layout) => new(
        layout.Rows,
        layout.Columns,
        layout.SeatsPerDesk,
        layout.AislesAfterColumns.ToList(),
        layout.DisabledSeats.ToHashSet(StringComparer.Ordinal),
        layout.AccessibleSeats.ToHashSet(StringComparer.Ordinal));

    public static SolverRule ToSolverRule(Rule rule) => new(
        rule.Id.Value,
        EnumMapper.Map<Domain.Rules.RuleType, Solver.Model.RuleType>(rule.Type),
        rule.IsHard,
        rule.Weight,
        ToSolverParameters(rule.Parameters));

    public static SolverParameters.RuleParameters ToSolverParameters(DomainParameters.RuleParameters parameters) => parameters switch
    {
        DomainParameters.SeparateByAttributeParameters p => new SolverParameters.SeparateByAttributeParameters(
            p.AttributeKey,
            EnumMapper.Map<Domain.Rules.RuleScope, Solver.Model.RuleScope>(p.Scope),
            p.MaxPerScope),
        DomainParameters.NoAdjacentByAttributeParameters p => new SolverParameters.NoAdjacentByAttributeParameters(
            p.AttributeKey,
            p.Adjacency.Select(MapAdjacency).ToList(),
            p.SeatLevelSide),
        DomainParameters.AccessibleSeatingParameters => new SolverParameters.AccessibleSeatingParameters(),
        DomainParameters.MinRoomFillParameters p => new SolverParameters.MinRoomFillParameters(p.MinFillRatio),
        DomainParameters.KeepTogetherInCenterParameters p => new SolverParameters.KeepTogetherInCenterParameters(p.AttributeKey),
        DomainParameters.SpreadInRoomParameters p => new SolverParameters.SpreadInRoomParameters(p.Adjacency.Select(MapAdjacency).ToList()),
        DomainParameters.FillFromFrontParameters => new SolverParameters.FillFromFrontParameters(),
        _ => throw new ArgumentOutOfRangeException(nameof(parameters), parameters.GetType().Name, "Unsupported rule parameters"),
    };

    public static SolveDiagnostic ToDomainDiagnostic(SolverDiagnostic diagnostic) => new(
        EnumMapper.Map<Solver.Model.DiagnosticCode, Domain.SolveRuns.DiagnosticCode>(diagnostic.Code),
        EnumMapper.Map<Solver.Model.DiagnosticSeverity, Domain.SolveRuns.DiagnosticSeverity>(diagnostic.Severity),
        diagnostic.RuleId is { } ruleId ? new RuleId(ruleId) : null,
        diagnostic.Arguments.ToDictionary(
            x => EnumMapper.Map<Solver.Model.DiagnosticArgument, Domain.SolveRuns.DiagnosticArgument>(x.Key),
            x => x.Value));

    public static SolverDiagnostic ToSolverDiagnostic(SolveDiagnostic diagnostic) => new(
        EnumMapper.Map<Domain.SolveRuns.DiagnosticCode, Solver.Model.DiagnosticCode>(diagnostic.Code),
        EnumMapper.Map<Domain.SolveRuns.DiagnosticSeverity, Solver.Model.DiagnosticSeverity>(diagnostic.Severity),
        diagnostic.RuleId?.Value,
        diagnostic.Arguments.ToDictionary(
            x => EnumMapper.Map<Domain.SolveRuns.DiagnosticArgument, Solver.Model.DiagnosticArgument>(x.Key),
            x => x.Value));

    public static SolveRunStats ToDomainStats(SolveStats stats) => new()
    {
        PrecheckMs = stats.PrecheckMs,
        Stage1Ms = stats.Stage1Ms,
        Stage2Ms = stats.Stage2Ms,
        AlternativesMs = stats.AlternativesMs,
        FeedbackIterations = stats.FeedbackIterations,
        Classes = stats.Classes,
        Variables = stats.Variables,
        Constraints = stats.Constraints,
    };

    public static AllocationObjective ToDomainObjective(ObjectiveBreakdown objective) => new()
    {
        RoomsUsed = objective.RoomsUsed,
        Cost = objective.Cost,
        SoftPenalty = objective.SoftPenalty,
        PenaltyByRule = objective.PenaltyByRule.ToDictionary(x => x.Key, x => x.Value),
    };

    public static Domain.Allocations.ViolationKind ToDomainViolationKind(Solver.Model.ViolationKind kind)
        => EnumMapper.Map<Solver.Model.ViolationKind, Domain.Allocations.ViolationKind>(kind);

    /// <summary>Maps a pipeline outcome to the persisted run status.</summary>
    public static SolveRunStatus ToRunStatus(SolveStatus status) => status switch
    {
        SolveStatus.Optimal or SolveStatus.Feasible => SolveRunStatus.Succeeded,
        SolveStatus.Infeasible => SolveRunStatus.Infeasible,
        SolveStatus.Cancelled => SolveRunStatus.Cancelled,
        _ => SolveRunStatus.Failed,
    };

    private static Solver.Model.AdjacencyKind MapAdjacency(Domain.Rules.AdjacencyKind kind)
        => EnumMapper.Map<Domain.Rules.AdjacencyKind, Solver.Model.AdjacencyKind>(kind);
}