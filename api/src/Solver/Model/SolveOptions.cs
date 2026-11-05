namespace Solver.Model;

public sealed record SolveOptions
{
    public int Alternatives { get; init; } = SolverDefaults.Alternatives;

    /// <summary>Minimum share of participants that must move between alternatives.</summary>
    public double DiversityRatio { get; init; } = SolverDefaults.DiversityRatio;

    /// <summary>Alternatives may cost up to (1 + CostSlack) of the best cost.</summary>
    public double CostSlack { get; init; } = SolverDefaults.CostSlack;

    public int Stage1TimeLimitSeconds { get; init; } = SolverDefaults.Stage1TimeLimitSeconds;

    public int Stage2TimeLimitSecondsPerRoom { get; init; } = SolverDefaults.Stage2TimeLimitSecondsPerRoom;

    public int MaxFeedbackIterations { get; init; } = SolverDefaults.MaxFeedbackIterations;

    /// <summary>CP-SAT workers; 0 means all cores.</summary>
    public int NumWorkers { get; init; } = SolverDefaults.NumWorkers;

    public int RandomSeed { get; init; } = SolverDefaults.RandomSeed;

    public ObjectiveMode ObjectiveMode { get; init; } = ObjectiveMode.CostThenSoft;
}