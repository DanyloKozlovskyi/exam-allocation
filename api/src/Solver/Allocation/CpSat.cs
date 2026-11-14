using System.Diagnostics;
using System.Globalization;
using Google.OrTools.Sat;
using Solver.Model;

namespace Solver.Allocation;

/// <summary>CP-SAT parameter handling shared by WS-A models (04 §11).</summary>
internal static class CpSat
{
    public const string LogEnvironmentVariable = "EXAM_SOLVER_LOG";
    public const string LogEnabledValue = "1";
    public const double MinTimeLimitSeconds = 0.05;

    private const string NumWorkersParameter = "num_workers";
    private const string MaxTimeParameter = "max_time_in_seconds";
    private const string RandomSeedParameter = "random_seed";
    private const string LogSearchParameter = "log_search_progress";

    public static int Workers(int configured) => configured <= 0 ? Environment.ProcessorCount : configured;

    public static string Parameters(int numWorkers, double timeLimitSeconds, int randomSeed)
    {
        var log = Environment.GetEnvironmentVariable(LogEnvironmentVariable) == LogEnabledValue;
        var seconds = Math.Max(MinTimeLimitSeconds, timeLimitSeconds);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{NumWorkersParameter}:{numWorkers} {MaxTimeParameter}:{seconds:0.###} {RandomSeedParameter}:{randomSeed} {LogSearchParameter}:{(log ? "true" : "false")}");
    }

    public static SolveStatus Map(CpSolverStatus status, bool cancelled) => status switch
    {
        _ when cancelled => SolveStatus.Cancelled,
        CpSolverStatus.Optimal => SolveStatus.Optimal,
        CpSolverStatus.Feasible => SolveStatus.Feasible,
        CpSolverStatus.Infeasible => SolveStatus.Infeasible,
        CpSolverStatus.ModelInvalid => SolveStatus.Failed,
        _ => SolveStatus.NoSolutionInTime,
    };

    public static bool HasSolution(CpSolverStatus status) => status is CpSolverStatus.Optimal or CpSolverStatus.Feasible;

    /// <summary>Solves with cancellation wired to <see cref="CpSolver.StopSearch"/>.</summary>
    public static CpSolverStatus Solve(CpSolver solver, CpModel model, SolutionCallback? callback, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return CpSolverStatus.Unknown;
        }

        using var registration = cancellationToken.Register(solver.StopSearch);
        return solver.Solve(model, callback);
    }
}

/// <summary>Reports objective and bound at most every <see cref="SolverDefaults.ProgressThrottleMilliseconds"/>.</summary>
internal sealed class ThrottledProgressCallback(IProgress<SolveProgress> progress, SolveStage stage) : CpSolverSolutionCallback
{
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();
    private long lastReportMs = -SolverDefaults.ProgressThrottleMilliseconds;

    public override void OnSolutionCallback()
    {
        var now = stopwatch.ElapsedMilliseconds;
        if (now - lastReportMs < SolverDefaults.ProgressThrottleMilliseconds)
        {
            return;
        }

        lastReportMs = now;
        progress.Report(new SolveProgress(stage, null, ObjectiveValue(), BestObjectiveBound(), null, null, null));
    }
}