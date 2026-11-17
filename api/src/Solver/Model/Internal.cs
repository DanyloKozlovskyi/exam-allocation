namespace Solver.Model;

/// <summary>Interchangeable non-pinned participants (identical values for every rule-referenced attribute).</summary>
public sealed record ParticipantClass(
    int Index,
    IReadOnlyDictionary<string, string> Values,
    bool NeedsAccessibleSeat,
    IReadOnlyList<Guid> ParticipantIds)
{
    public int Size => ParticipantIds.Count;
}

public sealed record SolverSeat(string Key, int Row, int Column, int SeatIndex, bool IsAccessible);

/// <summary>Undirected edge between two seat indices of a room (A &lt; B).</summary>
public sealed record SeatEdge(int A, int B);

public sealed record RoomClassCount(int ClassIndex, Guid RoomId, int Count);

/// <summary>Stage-1 cut produced by the feedback loop: at most MaxCount participants with Value of AttributeKey in RoomId.</summary>
public sealed record Stage1Cut(Guid RoomId, string AttributeKey, string Value, int MaxCount);

public sealed record Stage1Request(
    SolverInput Input,
    IReadOnlyList<ParticipantClass> Classes,
    IReadOnlyList<Stage1Cut> Cuts,
    IReadOnlyList<IReadOnlyList<RoomClassCount>> PreviousSolutions,
    long? CostBound,
    IReadOnlyList<RoomClassCount>? Hint);

public sealed record Stage1Result(
    SolveStatus Status,
    IReadOnlyList<RoomClassCount> Counts,
    ObjectiveBreakdown? Objective,
    IReadOnlyList<Diagnostic> Diagnostics,
    SolveStats Stats);

/// <summary>Seat taken by a class member or by a pinned participant.</summary>
public sealed record ClassSeatAssignment(int? ClassIndex, Guid? PinnedParticipantId, string SeatKey);

public sealed record SeatingRequest(
    SolverRoom Room,
    IReadOnlyList<ParticipantClass> Classes,
    IReadOnlyList<RoomClassCount> Counts,
    IReadOnlyList<SolverRule> Rules,
    IReadOnlyList<SolverPin> Pins,
    IReadOnlyList<SolverParticipant> PinnedParticipants,
    int RandomSeed,
    int TimeLimitSeconds,
    IReadOnlyList<IReadOnlyList<ClassSeatAssignment>> PreviousSeatings);

public sealed record SeatingResult(
    SolveStatus Status,
    IReadOnlyList<ClassSeatAssignment> Assignments,
    long Penalty,
    IReadOnlyDictionary<Guid, long> PenaltyByRule);

/// <param name="Cuts">Feedback cuts learned for the best allocation; every stage-1 alternative starts with them (04 §8, R-20).</param>
public sealed record AlternativesRequest(
    SolverInput Input,
    IReadOnlyList<ParticipantClass> Classes,
    Stage1Result Best,
    SolvedAllocation BestAllocation,
    IReadOnlyList<Stage1Cut>? Cuts = null);

public sealed record Violation(
    ViolationKind Kind,
    bool IsHard,
    Guid? RuleId,
    Guid? ParticipantId,
    Guid? RoomId,
    string? SeatKey);

public sealed record ValidationReport(
    bool IsValid,
    IReadOnlyList<Violation> Violations,
    long SoftPenalty,
    IReadOnlyDictionary<Guid, long> PenaltyByRule);