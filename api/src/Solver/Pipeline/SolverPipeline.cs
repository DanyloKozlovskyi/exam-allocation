using System.Diagnostics;
using Solver.Allocation;
using Solver.Alternatives;
using Solver.Expansion;
using Solver.Model;
using Solver.Precheck;
using Solver.Seating;
using Solver.Validation;

namespace Solver.Pipeline;

/// <summary>End-to-end solve: precheck → classes → stage 1 ⇄ stage 2 (feedback) → expansion → validation → alternatives (04 §3, §6, §8a).</summary>
public sealed class SolverPipeline(
    IPrecheck precheck,
    IClassBuilder classBuilder,
    IStage1Solver stage1Solver,
    IConflictExplainer conflictExplainer,
    ISeatingSolver seatingSolver,
    IExpander expander,
    IAllocationValidator validator,
    IAlternativesGenerator alternativesGenerator) : ISolverPipeline
{
    public async Task<SolverOutput> RunAsync(SolverInput input, IProgress<SolveProgress> progress, CancellationToken cancellationToken)
    {
        var run = new RunState();
        try
        {
            var output = await RunCoreAsync(input, progress, run, cancellationToken);
            if (cancellationToken.IsCancellationRequested && output.Status != SolveStatus.Cancelled)
            {
                return run.Output(SolveStatus.Cancelled);
            }

            progress.Report(new SolveProgress(SolveStage.Done, 100, null, null, null, null, output.Allocations.Count > 0 ? output.Allocations.Count - 1 : null));
            return output;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return run.Output(SolveStatus.Cancelled);
        }
    }

    private async Task<SolverOutput> RunCoreAsync(SolverInput input, IProgress<SolveProgress> progress, RunState run, CancellationToken cancellationToken)
    {
        // 1. Precheck
        progress.Report(new SolveProgress(SolveStage.Precheck, null, null, null, null, null, null));
        var stopwatch = Stopwatch.StartNew();
        run.Diagnostics.AddRange(precheck.Run(input, cancellationToken));
        run.Stats = run.Stats with { PrecheckMs = stopwatch.ElapsedMilliseconds };
        if (run.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return run.Output(SolveStatus.Infeasible);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 2. Classes
        var classes = classBuilder.Build(input);
        run.Stats = run.Stats with { Classes = classes.Count };

        // 3–4. Stage 1 ⇄ stage 2 with feedback cuts
        var realizer = new AllocationRealizer(seatingSolver, expander);
        var cuts = new List<Stage1Cut>();
        IReadOnlyList<RoomClassCount>? hint = null;
        Stage1Result stage1;
        Realization realization;
        for (var iteration = 0; ; iteration++)
        {
            progress.Report(new SolveProgress(iteration == 0 ? SolveStage.Stage1 : SolveStage.Feedback, null, null, null, null, null, null));
            var request = new Stage1Request(input, classes, cuts.ToList(), [], null, hint);
            stage1 = await Task.Run(() => stage1Solver.Solve(request, progress, cancellationToken), CancellationToken.None);
            run.AddStage1(stage1);
            cancellationToken.ThrowIfCancellationRequested();
            if (stage1.Status == SolveStatus.Infeasible)
            {
                run.Diagnostics.AddRange(cuts.Count == 0
                    ? conflictExplainer.Explain(request with { Hint = null }, cancellationToken)
                    : cuts.Select(AllocationRealizer.SeatingInfeasible));
                return run.Output(SolveStatus.Infeasible);
            }

            if (stage1.Status is not (SolveStatus.Optimal or SolveStatus.Feasible))
            {
                return run.Output(stage1.Status);
            }

            realization = await realizer.SeatAsync(input, classes, stage1.Counts, input.Options.RandomSeed, null, progress, cancellationToken);
            run.Stats = run.Stats with { Stage2Ms = run.Stats.Stage2Ms + realization.ElapsedMs };
            cancellationToken.ThrowIfCancellationRequested();
            if (realization.Cancelled)
            {
                return run.Output(SolveStatus.Cancelled);
            }

            if (realization.FailedRooms.Count == 0)
            {
                break;
            }

            var (newCuts, unresolved) = AllocationRealizer.BuildCuts(input, classes, stage1.Counts, realization.FailedRooms);
            if (unresolved.Count > 0 || iteration >= input.Options.MaxFeedbackIterations)
            {
                run.Diagnostics.AddRange(unresolved);
                run.Diagnostics.AddRange(newCuts.Select(AllocationRealizer.SeatingInfeasible));
                return run.Output(SolveStatus.Infeasible);
            }

            cuts.AddRange(newCuts);
            hint = stage1.Counts;
            run.Stats = run.Stats with { FeedbackIterations = run.Stats.FeedbackIterations + 1 };
        }

        // 5. Expansion
        progress.Report(new SolveProgress(SolveStage.Expanding, null, null, null, null, null, null));
        var items = realizer.Expand(input, classes, realization, 0);
        var best = new SolvedAllocation(0, items, AllocationRealizer.Objective(input, stage1.Objective, realization, items));

        // 6. Post-validation
        if (!IsValid(input, best, run))
        {
            return run.Output(SolveStatus.Failed);
        }

        run.Allocations.Add(best);
        var status = stage1.Status == SolveStatus.Optimal && realization.AllOptimal ? SolveStatus.Optimal : SolveStatus.Feasible;

        // 7. Alternatives
        if (input.Options.Alternatives > 0)
        {
            progress.Report(new SolveProgress(SolveStage.Alternatives, 0, null, null, null, null, 0));
            stopwatch.Restart();
            var request = new AlternativesRequest(input, classes, stage1, best, cuts.ToList());
            var alternatives = await Task.Run(() => alternativesGenerator.Generate(request, progress, cancellationToken), CancellationToken.None);
            run.Stats = run.Stats with { AlternativesMs = stopwatch.ElapsedMilliseconds };
            cancellationToken.ThrowIfCancellationRequested();
            run.Diagnostics.AddRange(alternatives.Diagnostics);
            foreach (var alternative in alternatives.Allocations)
            {
                if (IsValid(input, alternative, run, DiagnosticSeverity.Warning))
                {
                    run.Allocations.Add(alternative with { Index = run.Allocations.Count });
                }
            }
        }

        return run.Output(status);
    }

    private bool IsValid(SolverInput input, SolvedAllocation allocation, RunState run, DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        var report = validator.Validate(input, allocation.Items);
        if (report.IsValid)
        {
            return true;
        }

        var hard = report.Violations.Where(v => v.IsHard).ToList();
        run.Diagnostics.Add(DiagnosticFactory.Create(
            DiagnosticCode.InternalValidationFailed,
            severity,
            hard.Select(v => v.RuleId).FirstOrDefault(id => id is not null),
            (DiagnosticArgument.Count, report.Violations.Count),
            (DiagnosticArgument.Reason, DiagnosticFactory.FormatList(report.Violations.Select(v => v.Kind).Distinct())),
            (DiagnosticArgument.Value, allocation.Index)));
        return false;
    }

    private sealed class RunState
    {
        public List<Diagnostic> Diagnostics { get; } = [];

        public List<SolvedAllocation> Allocations { get; } = [];

        public SolveStats Stats { get; set; } = new();

        public void AddStage1(Stage1Result result)
        {
            Diagnostics.AddRange(result.Diagnostics);
            Stats = Stats with
            {
                Stage1Ms = Stats.Stage1Ms + result.Stats.Stage1Ms,
                Variables = result.Stats.Variables,
                Constraints = result.Stats.Constraints,
            };
        }

        public SolverOutput Output(SolveStatus status)
            => new(status, status is SolveStatus.Optimal or SolveStatus.Feasible ? Allocations.ToList() : [], Diagnostics.ToList(), Stats);
    }
}