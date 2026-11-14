using Solver.Model;

namespace Solver.Alternatives;

/// <summary>Why fewer alternatives than requested were produced; rendered as <see cref="DiagnosticArgument.Reason"/>.</summary>
public enum AlternativesLimitReason
{
    TimeBudgetExhausted = 1,
    NoDistinctAllocation = 2,
    Cancelled = 3,
}

public sealed record AlternativesResult(IReadOnlyList<SolvedAllocation> Allocations, IReadOnlyList<Diagnostic> Diagnostics);