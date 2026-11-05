namespace Solver.Model;

public sealed record SolverInput(
    IReadOnlyList<SolverRoom> Rooms,
    IReadOnlyList<SolverParticipant> Participants,
    IReadOnlyList<SolverRule> Rules,
    IReadOnlyList<SolverPin> Pins,
    SolveOptions Options);

public sealed record SolverRoom(Guid RoomId, Guid CenterId, decimal Cost, RoomLayoutModel Layout);

public sealed record RoomLayoutModel(
    int Rows,
    int Columns,
    int SeatsPerDesk,
    IReadOnlyList<int> AislesAfterColumns,
    IReadOnlySet<string> DisabledSeats,
    IReadOnlySet<string> AccessibleSeats);

/// <param name="Attributes">Resolved attribute key to value; keys come from the rules, never from solver code.</param>
/// <param name="SortKey">"LastName FirstName MiddleName" for deterministic expansion.</param>
public sealed record SolverParticipant(
    Guid ParticipantId,
    IReadOnlyDictionary<string, string> Attributes,
    bool NeedsAccessibleSeat,
    string SortKey);

public sealed record SolverRule(Guid RuleId, RuleType Type, bool IsHard, int Weight, RuleParameters Parameters);

public sealed record SolverPin(Guid ParticipantId, Guid RoomId, string? SeatKey);