using System.Diagnostics;
using Google.OrTools.Sat;
using Solver.Layout;
using Solver.Model;

namespace Solver.Allocation;

/// <summary>CP-SAT helpers of the stage-1 models (04 §11): worker count, status mapping, cancellable solve. Parameters come from <see cref="CpSatParameters"/>.</summary>
internal static class CpSat
{
    /// <summary>Smallest time limit passed to CP-SAT (see <see cref="CpSatParameters.MinTimeLimitSeconds"/>).</summary>
    public const double MinTimeLimitSeconds = CpSatParameters.MinTimeLimitSeconds;

    public static int Workers(int configured) => configured <= 0 ? Environment.ProcessorCount : configured;

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