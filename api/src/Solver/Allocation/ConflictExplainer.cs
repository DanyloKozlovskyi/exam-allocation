using System.Diagnostics;
using Google.OrTools.Sat;
using Solver.Layout;
using Solver.Model;
using Solver.Precheck;

namespace Solver.Allocation;

/// <summary>Explains an infeasible stage 1 by a minimized conflict core of hard rules and center capacities (04 §7.2).</summary>
public sealed class ConflictExplainer(ISeatGenerator seatGenerator, IIndependentSetCalculator independentSetCalculator) : IConflictExplainer
{
    private const int ExplainWorkers = 1;

    /// <summary>Test seam: time limit of the assumption solve instead of <see cref="SolveOptions.Stage1TimeLimitSeconds"/>.</summary>
    internal double? ProofTimeLimitSeconds { get; init; }

    public IReadOnlyList<Diagnostic> Explain(Stage1Request request, CancellationToken cancellationToken)
    {
        var options = request.Input.Options;
        var context = Stage1Context.Create(request.Input, request.Classes, seatGenerator, independentSetCalculator);
        var built = Stage1ModelBuilder.Build(context, request, explain: true, cancellationToken);
        if (built.Enforcement.Count == 0)
        {
            return [];
        }

        using var solver = new CpSolver();
        var literals = built.Enforcement.Select(e => e.Literal).ToList();
        built.Model.AddAssumptions(literals);
        solver.StringParameters = CpSatParameters.Build(ExplainWorkers, ProofTimeLimitSeconds ?? options.Stage1TimeLimitSeconds, options.RandomSeed);
        var status = CpSat.Solve(solver, built.Model, null, cancellationToken);
        if (status == CpSolverStatus.Unknown && !cancellationToken.IsCancellationRequested)
        {
            // Not proven within the limit: stage 1 already proved the whole model infeasible, so all literals form a core (04 §7.2).
            return [Core(built.Enforcement, ConflictCoreReason.Unminimized)];
        }

        if (status != CpSolverStatus.Infeasible)
        {
            return [];
        }

        var byIndex = built.Enforcement.ToDictionary(e => e.Literal.GetIndex());
        var core = solver.SufficientAssumptionsForInfeasibility()
            .Where(byIndex.ContainsKey)
            .Select(i => byIndex[i])
            .ToList();
        if (core.Count == 0)
        {
            core = built.Enforcement.ToList();
        }

        core = Minimize(built.Model, solver, core, options.RandomSeed, cancellationToken);
        return [Core(core, null)];
    }

    private static Diagnostic Core(IReadOnlyList<(BoolVar Literal, ConflictItem Item)> core, ConflictCoreReason? reason)
    {
        var ruleIds = core.Select(c => c.Item.RuleId).OfType<Guid>().Distinct().ToList();
        var centerIds = core.Select(c => c.Item.CenterId).OfType<Guid>().Distinct().ToList();
        var arguments = new List<(DiagnosticArgument, object)> { (DiagnosticArgument.RuleIds, DiagnosticFactory.FormatList(ruleIds)) };
        if (centerIds.Count > 0)
        {
            arguments.Add((DiagnosticArgument.CenterId, DiagnosticFactory.FormatList(centerIds)));
        }

        if (reason is { } value)
        {
            arguments.Add((DiagnosticArgument.Reason, value.ToString()));
        }

        return DiagnosticFactory.Error(DiagnosticCode.ConflictCore, ruleIds.Count == 1 ? ruleIds[0] : null, [.. arguments]);
    }

    /// <summary>Deletion-based minimization: drop a literal whenever the rest stays infeasible.</summary>
    private static List<(BoolVar Literal, ConflictItem Item)> Minimize(
        CpModel model,
        CpSolver solver,
        List<(BoolVar Literal, ConflictItem Item)> core,
        int seed,
        CancellationToken cancellationToken)
    {
        var budget = Stopwatch.StartNew();
        var current = core.ToList();
        for (var i = 0; i < current.Count;)
        {
            var remaining = SolverDefaults.ConflictCoreMinimizationSeconds - budget.Elapsed.TotalSeconds;
            if (remaining <= 0 || cancellationToken.IsCancellationRequested || current.Count == 1)
            {
                break;
            }

            var candidate = current.Where((_, index) => index != i).ToList();
            model.ClearAssumptions();
            model.AddAssumptions(candidate.Select(c => c.Literal));
            solver.StringParameters = CpSatParameters.Build(ExplainWorkers, remaining, seed);
            if (CpSat.Solve(solver, model, null, cancellationToken) == CpSolverStatus.Infeasible)
            {
                current = candidate;
            }
            else
            {
                i++;
            }
        }

        return current;
    }
}

/// <summary>Why a <see cref="DiagnosticCode.ConflictCore"/> is not a minimized core; rendered as <see cref="DiagnosticArgument.Reason"/>.</summary>
public enum ConflictCoreReason
{
    /// <summary>The assumption solve did not prove infeasibility in time; the core lists every hard rule and center.</summary>
    Unminimized = 1,
}