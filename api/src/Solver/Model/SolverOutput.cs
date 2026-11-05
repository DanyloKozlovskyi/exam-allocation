namespace Solver.Model;

public sealed record SolverOutput(
    SolveStatus Status,
    IReadOnlyList<SolvedAllocation> Allocations,
    IReadOnlyList<Diagnostic> Diagnostics,
    SolveStats Stats);

/// <param name="Index">0 = best, 1..N = alternatives.</param>
public sealed record SolvedAllocation(int Index, IReadOnlyList<SeatAssignment> Items, ObjectiveBreakdown Objective);

public sealed record SeatAssignment(Guid ParticipantId, Guid RoomId, string SeatKey);

public sealed record ObjectiveBreakdown(int RoomsUsed, decimal Cost, long SoftPenalty, IReadOnlyDictionary<Guid, long> PenaltyByRule);

/// <summary>Machine-readable diagnostic; the message is rendered from resources by <see cref="Code"/>.</summary>
public sealed record Diagnostic(
    DiagnosticCode Code,
    DiagnosticSeverity Severity,
    Guid? RuleId,
    IReadOnlyDictionary<DiagnosticArgument, string> Arguments);

public sealed record SolveStats
{
    public long PrecheckMs { get; init; }

    public long Stage1Ms { get; init; }

    public long Stage2Ms { get; init; }

    public long AlternativesMs { get; init; }

    public int FeedbackIterations { get; init; }

    public int Classes { get; init; }

    public int Variables { get; init; }

    public int Constraints { get; init; }
}