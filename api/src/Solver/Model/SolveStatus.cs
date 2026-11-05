namespace Solver.Model;

public enum SolveStatus
{
    Optimal = 1,
    Feasible = 2,
    Infeasible = 3,
    NoSolutionInTime = 4,
    Cancelled = 5,
    Failed = 6,
}