namespace Domain.SolveRuns;

public enum SolveRunStatus
{
    Queued = 1,
    Running = 2,
    Succeeded = 3,
    Infeasible = 4,
    Failed = 5,
    Cancelled = 6,
}