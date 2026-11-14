using System.Diagnostics;
using Solver.Allocation;
using Solver.Expansion;
using Solver.Model;
using Solver.Pipeline;
using Solver.Precheck;
using Solver.Seating;

namespace Solver.Alternatives;

/// <summary>Alternatives loop of 04 §8: stage-1 diversity first, seat-level diversity as fallback.</summary>
public sealed class AlternativesGenerator(IStage1Solver stage1Solver, ISeatingSolver seatingSolver, IExpander expander) : IAlternativesGenerator
{
    private const int BudgetMultiplier = 2;
    private const int MinStage1Seconds = 1;

    public AlternativesResult Generate(AlternativesRequest request, IProgress<SolveProgress> progress, CancellationToken cancellationToken)
    {
        var input = request.Input;
        var requested = input.Options.Alternatives;
        if (requested <= 0)
        {
            return new AlternativesResult([], []);
        }

        var state = new GenerationState(request, new AllocationRealizer(seatingSolver, expander));
        AlternativesLimitReason? reason = null;
        for (var index = 1; index <= requested; index++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                reason = AlternativesLimitReason.Cancelled;
                break;
            }

            if (state.RemainingSeconds <= 0)
            {
                reason = AlternativesLimitReason.TimeBudgetExhausted;
                break;
            }

            var attempt = state.SeatLevel ? null : TryStage1Alternative(state, index, progress, cancellationToken);
            if (attempt is { Outcome: AttemptOutcome.OutOfTime })
            {
                reason = cancellationToken.IsCancellationRequested ? AlternativesLimitReason.Cancelled : AlternativesLimitReason.TimeBudgetExhausted;
                break;
            }

            if (attempt is null or { Outcome: AttemptOutcome.Exhausted })
            {
                state.SeatLevel = true;
                attempt = TrySeatLevelAlternative(state, index, progress, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                reason = AlternativesLimitReason.Cancelled;
                break;
            }

            if (attempt.Allocation is null || !state.Hashes.Add(AllocationRealizer.Hash(attempt.Allocation.Items)))
            {
                reason = AlternativesLimitReason.NoDistinctAllocation;
                break;
            }

            state.Accept(attempt);
            progress.Report(new SolveProgress(SolveStage.Alternatives, 100.0 * state.Allocations.Count / requested, null, null, null, null, state.Allocations.Count));
        }

        var diagnostics = new List<Diagnostic>();
        if (state.Allocations.Count < requested)
        {
            diagnostics.Add(DiagnosticFactory.Warning(
                DiagnosticCode.AlternativesLimited,
                null,
                (DiagnosticArgument.Reason, (reason ?? AlternativesLimitReason.NoDistinctAllocation).ToString()),
                (DiagnosticArgument.Count, state.Allocations.Count),
                (DiagnosticArgument.Limit, requested)));
        }

        return new AlternativesResult(state.Allocations, diagnostics);
    }

    private Attempt TryStage1Alternative(GenerationState state, int index, IProgress<SolveProgress> progress, CancellationToken cancellationToken)
    {
        var input = state.Request.Input;
        for (var iteration = 0; iteration <= input.Options.MaxFeedbackIterations; iteration++)
        {
            var seconds = (int)Math.Max(MinStage1Seconds, Math.Ceiling(Math.Min(input.Options.Stage1TimeLimitSeconds, state.RemainingSeconds)));
            var limited = input with { Options = input.Options with { Stage1TimeLimitSeconds = seconds } };
            var stage1 = stage1Solver.Solve(
                new Stage1Request(limited, state.Request.Classes, state.Cuts, state.PreviousCounts, state.CostBound, null),
                progress,
                cancellationToken);
            if (stage1.Status == SolveStatus.Infeasible)
            {
                return new Attempt(AttemptOutcome.Exhausted, null, null, null);
            }

            if (stage1.Status is not (SolveStatus.Optimal or SolveStatus.Feasible))
            {
                return new Attempt(AttemptOutcome.OutOfTime, null, null, null);
            }

            var realization = state.Realizer
                .SeatAsync(input, state.Request.Classes, stage1.Counts, input.Options.RandomSeed + index, null, progress, cancellationToken)
                .GetAwaiter()
                .GetResult();
            if (realization.Cancelled)
            {
                return new Attempt(AttemptOutcome.OutOfTime, null, null, null);
            }

            if (realization.FailedRooms.Count == 0)
            {
                var items = state.Realizer.Expand(input, state.Request.Classes, realization, index);
                var objective = AllocationRealizer.Objective(input, stage1.Objective, realization, items);
                return new Attempt(AttemptOutcome.Found, new SolvedAllocation(index, items, objective), stage1.Counts, null);
            }

            var (cuts, unresolved) = AllocationRealizer.BuildCuts(input, state.Request.Classes, stage1.Counts, realization.FailedRooms);
            if (unresolved.Count > 0)
            {
                return new Attempt(AttemptOutcome.Exhausted, null, null, null);
            }

            state.Cuts.AddRange(cuts);
        }

        return new Attempt(AttemptOutcome.Exhausted, null, null, null);
    }

    private Attempt TrySeatLevelAlternative(GenerationState state, int index, IProgress<SolveProgress> progress, CancellationToken cancellationToken)
    {
        var input = state.Request.Input;
        var counts = state.Request.Best.Counts;
        var previous = state.SeatingsForBestCounts
            .SelectMany(s => s)
            .GroupBy(e => e.Key)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<IReadOnlyList<ClassSeatAssignment>>)g.Select(e => e.Value).ToList());
        var realization = state.Realizer
            .SeatAsync(input, state.Request.Classes, counts, input.Options.RandomSeed + index, previous, progress, cancellationToken)
            .GetAwaiter()
            .GetResult();
        if (realization.Cancelled || realization.FailedRooms.Count > 0)
        {
            return new Attempt(AttemptOutcome.Exhausted, null, null, null);
        }

        var items = state.Realizer.Expand(input, state.Request.Classes, realization, index);
        var objective = AllocationRealizer.Objective(input, state.Request.Best.Objective, realization, items);
        return new Attempt(AttemptOutcome.Found, new SolvedAllocation(index, items, objective), null, realization.SeatingByRoom);
    }

    private enum AttemptOutcome
    {
        Found = 1,
        Exhausted = 2,
        OutOfTime = 3,
    }

    private sealed record Attempt(
        AttemptOutcome Outcome,
        SolvedAllocation? Allocation,
        IReadOnlyList<RoomClassCount>? Counts,
        IReadOnlyDictionary<Guid, IReadOnlyList<ClassSeatAssignment>>? Seatings);

    private sealed class GenerationState
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private readonly double budgetSeconds;

        public GenerationState(AlternativesRequest request, AllocationRealizer realizer)
        {
            Request = request;
            Realizer = realizer;
            budgetSeconds = (double)request.Input.Options.Stage1TimeLimitSeconds * BudgetMultiplier;
            PreviousCounts.Add(request.Best.Counts);
            Hashes.Add(AllocationRealizer.Hash(request.BestAllocation.Items));
            SeatingsForBestCounts.Add(AllocationRealizer.ToClassSeatings(request.Input, request.Classes, request.BestAllocation.Items));
            var bestCost = request.Best.Objective?.Cost ?? request.BestAllocation.Objective.Cost;
            CostBound = (long)Math.Ceiling(Stage1ModelBuilder.ScaledCost(bestCost) * (1 + request.Input.Options.CostSlack));
        }

        public AlternativesRequest Request { get; }

        public AllocationRealizer Realizer { get; }

        public long CostBound { get; }

        public bool SeatLevel { get; set; }

        public List<SolvedAllocation> Allocations { get; } = [];

        public HashSet<string> Hashes { get; } = new(StringComparer.Ordinal);

        public List<IReadOnlyList<RoomClassCount>> PreviousCounts { get; } = [];

        public List<IReadOnlyDictionary<Guid, IReadOnlyList<ClassSeatAssignment>>> SeatingsForBestCounts { get; } = [];

        public List<Stage1Cut> Cuts { get; } = [];

        public double RemainingSeconds => budgetSeconds - stopwatch.Elapsed.TotalSeconds;

        public void Accept(Attempt attempt)
        {
            Allocations.Add(attempt.Allocation!);
            if (attempt.Counts is not null)
            {
                PreviousCounts.Add(attempt.Counts);
            }

            if (attempt.Seatings is not null)
            {
                SeatingsForBestCounts.Add(attempt.Seatings);
            }
        }
    }
}