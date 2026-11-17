namespace Domain.SolveRuns;

/// <summary>Machine-readable reason of a <see cref="SolveRunStatus.Failed"/> run (clients render text from it).</summary>
public enum SolveRunFailureReason
{
    /// <summary>The server stopped while the run was queued or running (restart recovery).</summary>
    Interrupted = 1,

    /// <summary>CP-SAT found no allocation within the time limit.</summary>
    NoSolutionInTime = 2,

    /// <summary>Unexpected solver or pipeline error; details in <see cref="SolveRun.ErrorMessage"/>.</summary>
    SolverError = 3,

    /// <summary>The input is too large for the solver (<see cref="DiagnosticCode.ModelTooLarge"/>); fixable by the user.</summary>
    ModelTooLarge = 4,
}