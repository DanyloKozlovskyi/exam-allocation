using Solver.Model;

namespace Solver.Pipeline;

/// <summary>Swallows progress of nested solves (e.g. per-alternative stage 1/2) so clients see one monotonic stage sequence.</summary>
public sealed class SilentProgress : IProgress<SolveProgress>
{
    public static readonly SilentProgress Instance = new();

    private SilentProgress()
    {
    }

    public void Report(SolveProgress value)
    {
    }
}