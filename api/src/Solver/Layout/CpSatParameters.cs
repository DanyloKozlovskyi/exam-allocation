using System.Globalization;

namespace Solver.Layout;

/// <summary>Builds CP-SAT text-format parameter strings (docs/04 §11) without scattering parameter names.</summary>
public static class CpSatParameters
{
    public const string NumWorkers = "num_workers";
    public const string MaxTimeInSeconds = "max_time_in_seconds";
    public const string RandomSeed = "random_seed";
    public const string LogSearchProgress = "log_search_progress";
    public const string LogEnvironmentVariable = "EXAM_SOLVER_LOG";
    public const string LogEnabledValue = "1";

    /// <summary>Time limits below this are raised to it (a zero or negative limit would not stop CP-SAT at once).</summary>
    public const double MinTimeLimitSeconds = 0.05;

    public static string Build(int numWorkers, double maxTimeInSeconds, int randomSeed)
    {
        var seconds = Math.Max(MinTimeLimitSeconds, maxTimeInSeconds);
        var log = string.Equals(Environment.GetEnvironmentVariable(LogEnvironmentVariable), LogEnabledValue, StringComparison.Ordinal);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{NumWorkers}:{numWorkers} {MaxTimeInSeconds}:{seconds:0.###} {RandomSeed}:{randomSeed} {LogSearchProgress}:{(log ? "true" : "false")}");
    }
}