namespace Solver.Model;

public sealed record SolveProgress(
    SolveStage Stage,
    double? Percent,
    double? Objective,
    double? BestBound,
    int? RoomsDone,
    int? RoomsTotal,
    int? AlternativesDone);