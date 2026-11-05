using Domain.SolveRuns;
using Solver.Model;
using DiagnosticArgument = Domain.SolveRuns.DiagnosticArgument;
using DiagnosticCode = Domain.SolveRuns.DiagnosticCode;
using DiagnosticSeverity = Domain.SolveRuns.DiagnosticSeverity;
using ObjectiveMode = Domain.SolveRuns.ObjectiveMode;

namespace Api.Dtos;

public record SolveOptionsDto(
    int? Alternatives,
    double? DiversityRatio,
    double? CostSlack,
    int? TimeLimitSeconds,
    ObjectiveMode? ObjectiveMode,
    int? Seed)
{
    public SolveRunOptions ToOptions()
    {
        var defaults = new SolveRunOptions();
        return new SolveRunOptions
        {
            Alternatives = Alternatives ?? defaults.Alternatives,
            DiversityRatio = DiversityRatio ?? defaults.DiversityRatio,
            CostSlack = CostSlack ?? defaults.CostSlack,
            TimeLimitSeconds = TimeLimitSeconds ?? defaults.TimeLimitSeconds,
            ObjectiveMode = ObjectiveMode ?? defaults.ObjectiveMode,
            Seed = Seed ?? defaults.Seed,
        };
    }
}

public record DiagnosticDto(
    DiagnosticCode Code,
    DiagnosticSeverity Severity,
    string Message,
    Guid? RuleId,
    IReadOnlyDictionary<DiagnosticArgument, string> Arguments);

public record SolveRunStatsDto(
    long PrecheckMs,
    long Stage1Ms,
    long Stage2Ms,
    long AlternativesMs,
    int FeedbackIterations,
    int Classes,
    int Variables,
    int Constraints)
{
    public static SolveRunStatsDto FromDomain(SolveRunStats stats) => new(
        stats.PrecheckMs, stats.Stage1Ms, stats.Stage2Ms, stats.AlternativesMs, stats.FeedbackIterations, stats.Classes, stats.Variables, stats.Constraints);
}

public record SolveRunDto(
    Guid Id,
    Guid SessionId,
    SolveRunStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    SolveRunStatsDto? Stats,
    IReadOnlyList<DiagnosticDto> Diagnostics,
    IReadOnlyList<Guid> AllocationIds,
    bool IsStale,
    string? ErrorMessage);

public record SolveProgressDto(
    Guid RunId,
    SolveStage Stage,
    double? Percent,
    double? Objective,
    double? BestBound,
    int? RoomsDone,
    int? RoomsTotal,
    int? AlternativesDone)
{
    public static SolveProgressDto FromModel(Guid runId, SolveProgress progress) => new(
        runId, progress.Stage, progress.Percent, progress.Objective, progress.BestBound, progress.RoomsDone, progress.RoomsTotal, progress.AlternativesDone);
}