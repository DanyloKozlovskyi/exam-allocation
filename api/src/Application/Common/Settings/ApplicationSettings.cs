using Solver.Model;

namespace Application.Common.Settings;

public class ApplicationSettings
{
    public SolverSettings Solver { get; set; } = new();

    public ImportSettings Import { get; set; } = new();

    public CorsSettings Cors { get; set; } = new();
}

public class SolverSettings
{
    public const int DefaultMaxConcurrentRuns = 1;

    public int MaxConcurrentRuns { get; set; } = DefaultMaxConcurrentRuns;

    public int NumWorkers { get; set; } = SolverDefaults.NumWorkers;

    public int Stage1TimeLimitSeconds { get; set; } = SolverDefaults.Stage1TimeLimitSeconds;

    public int Stage2TimeLimitSecondsPerRoom { get; set; } = SolverDefaults.Stage2TimeLimitSecondsPerRoom;

    public int MaxFeedbackIterations { get; set; } = SolverDefaults.MaxFeedbackIterations;

    public int DefaultAlternatives { get; set; } = SolverDefaults.Alternatives;
}

public class ImportSettings
{
    public const int DefaultMaxFileSizeMb = 20;
    public const int DefaultMaxRows = 100_000;
    public const int DefaultCacheMinutes = 60;

    public int MaxFileSizeMb { get; set; } = DefaultMaxFileSizeMb;

    public int MaxRows { get; set; } = DefaultMaxRows;

    public int CacheMinutes { get; set; } = DefaultCacheMinutes;
}

public class CorsSettings
{
    public IReadOnlyList<string> Origins { get; set; } = [];
}