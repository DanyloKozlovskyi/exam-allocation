namespace Domain.SolveRuns;

public sealed record SolveRunOptions
{
    public const int MinAlternatives = 0;
    public const int MaxAlternatives = 20;
    public const int DefaultAlternatives = 5;
    public const double DefaultDiversityRatio = 0.10;
    public const double DefaultCostSlack = 0.10;
    public const int DefaultTimeLimitSeconds = 60;
    public const int MinTimeLimitSeconds = 5;
    public const int MaxTimeLimitSeconds = 3600;
    public const int DefaultSeed = 42;

    public int Alternatives { get; init; } = DefaultAlternatives;

    public double DiversityRatio { get; init; } = DefaultDiversityRatio;

    public double CostSlack { get; init; } = DefaultCostSlack;

    public int TimeLimitSeconds { get; init; } = DefaultTimeLimitSeconds;

    public ObjectiveMode ObjectiveMode { get; init; } = ObjectiveMode.CostThenSoft;

    public int Seed { get; init; } = DefaultSeed;
}