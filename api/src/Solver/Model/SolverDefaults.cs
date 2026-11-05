namespace Solver.Model;

public static class SolverDefaults
{
    public const int Alternatives = 5;
    public const double DiversityRatio = 0.10;
    public const double CostSlack = 0.10;
    public const int Stage1TimeLimitSeconds = 60;
    public const int Stage2TimeLimitSecondsPerRoom = 5;
    public const int MaxFeedbackIterations = 5;
    public const int NumWorkers = 0;
    public const int RandomSeed = 42;
    public const int IndependentSetTimeLimitSeconds = 2;
    public const int ConflictCoreMinimizationSeconds = 10;
    public const long MaxStage1Cells = 2_000_000;
    public const int CostScale = 100;
    public const int WeightedCostMultiplier = 1000;
    public const double Stage1CostPhaseShare = 0.6;
    public const int ProgressThrottleMilliseconds = 500;
}