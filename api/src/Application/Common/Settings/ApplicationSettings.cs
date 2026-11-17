using Application.Imports.Models;
using Solver.Model;

namespace Application.Common.Settings;

public class ApplicationSettings
{
    public SolverSettings Solver { get; set; } = new();

    public ImportSettings Import { get; set; } = new();

    public CorsSettings Cors { get; set; } = new();
}

/// <summary>
/// Server-side solver tuning. Per-run choices (time limit, alternatives) come from the request's
/// <c>SolveOptionsDto</c> with defaults in <c>SolveRunOptions</c>; runs execute one at a time (single worker).
/// </summary>
public class SolverSettings
{
    public int NumWorkers { get; set; } = SolverDefaults.NumWorkers;

    public int Stage2TimeLimitSecondsPerRoom { get; set; } = SolverDefaults.Stage2TimeLimitSecondsPerRoom;

    public int MaxFeedbackIterations { get; set; } = SolverDefaults.MaxFeedbackIterations;
}

public class ImportSettings
{
    public const int DefaultMaxFileSizeMb = 20;
    public const int DefaultMaxRows = 100_000;
    public const int DefaultCacheMinutes = 60;

    /// <summary>Request body allowance on top of the file itself (multipart boundaries, form fields).</summary>
    public const long RequestOverheadBytes = ImportLimits.BytesPerMegabyte;

    public int MaxFileSizeMb { get; set; } = DefaultMaxFileSizeMb;

    public int MaxRows { get; set; } = DefaultMaxRows;

    public int CacheMinutes { get; set; } = DefaultCacheMinutes;

    public long MaxFileSizeBytes => MaxFileSizeMb * ImportLimits.BytesPerMegabyte;

    public long MaxRequestBodyBytes => MaxFileSizeBytes + RequestOverheadBytes;
}

public class CorsSettings
{
    public IReadOnlyList<string> Origins { get; set; } = [];
}