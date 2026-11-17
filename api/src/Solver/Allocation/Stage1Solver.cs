using System.Diagnostics;
using Google.OrTools.Sat;
using Solver.Layout;
using Solver.Model;
using Solver.Precheck;

namespace Solver.Allocation;

/// <summary>Stage 1: participant classes to rooms (04 §4), with the two-step solve of 04 §4.1 for the best allocation.</summary>
public sealed class Stage1Solver(ISeatGenerator seatGenerator, IIndependentSetCalculator independentSetCalculator) : IStage1Solver
{
    /// <summary>Share of the stage-1 limit for step S (room selection) of 04 §4.1.</summary>
    private const double SelectionShare = 0.1;

    /// <summary>Share of the stage-1 limit for step A (assignment on the selected rooms) of 04 §4.1.</summary>
    private const double AssignmentShare = 0.5;

    public Stage1Result Solve(Stage1Request request, IProgress<SolveProgress> progress, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var options = request.Input.Options;
        var context = Stage1Context.Create(request.Input, request.Classes, seatGenerator, independentSetCalculator);
        var cells = Stage1ModelBuilder.CountCells(context);
        if (cells > SolverDefaults.MaxStage1Cells)
        {
            var diagnostic = DiagnosticFactory.Error(
                DiagnosticCode.ModelTooLarge,
                null,
                (DiagnosticArgument.Size, cells),
                (DiagnosticArgument.Limit, SolverDefaults.MaxStage1Cells));
            return new Stage1Result(SolveStatus.Failed, [], null, [diagnostic], new SolveStats { Classes = request.Classes.Count, Stage1Ms = stopwatch.ElapsedMilliseconds });
        }

        var run = new RunContext(context, request, progress, stopwatch, cancellationToken);
        if (request.PreviousSolutions.Count > 0)
        {
            var built = Stage1ModelBuilder.Build(context, request, explain: false, cancellationToken);
            built.Model.Minimize(built.SoftPenalty);
            return run.Finish(built, run.Single(built, run.Remaining));
        }

        if (options.ObjectiveMode == ObjectiveMode.Weighted)
        {
            var built = Stage1ModelBuilder.Build(context, request, explain: false, cancellationToken);
            built.Model.Minimize((SolverDefaults.WeightedCostMultiplier * built.Cost) + built.SoftPenalty);
            return run.Finish(built, run.Single(built, run.Remaining));
        }

        return request.CostBound is null ? SolveTwoStep(run) : run.FinishLexicographic(Stage1ModelBuilder.Build(context, request, explain: false, cancellationToken));
    }

    /// <summary>Steps S, E, A, C and D of 04 §4.1.</summary>
    private static Stage1Result SolveTwoStep(RunContext run)
    {
        var context = run.Context;
        var request = run.Request;
        var token = run.CancellationToken;
        var costs = context.Rooms.Select(r => Stage1ModelBuilder.ScaledCost(r.Room.Cost)).ToArray();

        // Step S: room selection on room types → lower bound and selected rooms.
        var selection = RoomSelection.Solve(context, request.Cuts, costs, run.Workers, run.TimeLimit * SelectionShare, token);
        if (selection is null || token.IsCancellationRequested)
        {
            return run.FinishLexicographic(Stage1ModelBuilder.Build(context, request, explain: false, token));
        }

        // Step E: evaluate the greedy assignment; step A: the stage-1 model restricted to the selected rooms, with a complete hint.
        var greedy = GreedyAssignment.Assign(context, selection.Selected, request.Cuts, token);
        if (greedy.Complete && selection.Optimal)
        {
            // A complete greedy assignment at cost LB with no soft penalty is optimal: evaluate it without search.
            var evaluated = Stage1ModelBuilder.Build(context, request with { Hint = null }, explain: false, token, new Stage1BuildOptions(selection.Selected, selection.LowerBound, greedy.Counts, Fixed: true));
            evaluated.Model.Minimize(evaluated.SoftPenalty);
            var evaluation = run.Single(evaluated, run.TimeLimit * SelectionShare);
            if (evaluation.Solution is { Objective.SoftPenalty: 0 } && Stage1ModelBuilder.ScaledCost(evaluation.Solution.Objective.Cost) <= selection.LowerBound)
            {
                return run.Finish(evaluated, evaluation with { Optimal = true });
            }
        }

        var hint = request.Hint ?? greedy.Counts;
        var restricted = Stage1ModelBuilder.Build(context, request, explain: false, token, new Stage1BuildOptions(selection.Selected, selection.LowerBound, hint));
        var assignmentSeconds = Math.Min(run.TimeLimit * AssignmentShare, run.Remaining);
        Outcome assignment;
        if (selection.Optimal)
        {
            // Cost ≥ LB and only selected rooms: the cost is already fixed to its optimum, solve the soft part only.
            if (restricted.HasSoftPenalty)
            {
                restricted.Model.Minimize(restricted.SoftPenalty);
            }

            assignment = run.Single(restricted, assignmentSeconds);
        }
        else
        {
            assignment = run.Lexicographic(restricted, assignmentSeconds);
        }

        if (assignment.Solution is not { } solution)
        {
            if (token.IsCancellationRequested)
            {
                return run.Finish(restricted, assignment);
            }

            // Step D: fallback to the single model with the remaining time.
            var full = Stage1ModelBuilder.Build(context, request, explain: false, token, new Stage1BuildOptions(null, selection.LowerBound, null));
            return run.FinishLexicographic(full);
        }

        var costOptimal = selection.Optimal && Stage1ModelBuilder.ScaledCost(solution.Objective.Cost) <= selection.LowerBound;
        var softProven = !restricted.HasSoftPenalty || solution.Objective.SoftPenalty == 0;
        if (softProven || token.IsCancellationRequested || run.Remaining <= CpSat.MinTimeLimitSeconds)
        {
            return run.Finish(restricted, assignment with { Optimal = assignment.Optimal && costOptimal && softProven });
        }

        // Step C: soft penalty over all rooms at the same cost, hinted with step A.
        var global = Stage1ModelBuilder.Build(context, request with { Hint = solution.Counts }, explain: false, token, new Stage1BuildOptions(null, selection.LowerBound, null));
        global.Model.Add(global.Cost <= Stage1ModelBuilder.ScaledCost(solution.Objective.Cost));
        global.Model.Minimize(global.SoftPenalty);
        var check = run.Single(global, run.Remaining);
        if (check.Solution is { } better && better.Objective.SoftPenalty <= solution.Objective.SoftPenalty)
        {
            return run.Finish(global, check with { Optimal = check.Optimal && costOptimal });
        }

        return run.Finish(restricted, assignment with { Optimal = false });
    }

    /// <summary>A solved phase: the best solution found (if any), whether it was proven optimal, and the last CP-SAT status.</summary>
    private sealed record Outcome(Solution? Solution, bool Optimal, CpSolverStatus Status);

    private sealed record Solution(IReadOnlyList<RoomClassCount> Counts, ObjectiveBreakdown Objective);

    private sealed class RunContext(Stage1Context context, Stage1Request request, IProgress<SolveProgress> progress, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        private readonly ThrottledProgressCallback callback = new(progress, SolveStage.Stage1);

        public Stage1Context Context => context;

        public Stage1Request Request => request;

        public CancellationToken CancellationToken => cancellationToken;

        public int Workers { get; } = CpSat.Workers(request.Input.Options.NumWorkers);

        public double TimeLimit { get; } = request.Input.Options.Stage1TimeLimitSeconds;

        public double Remaining => Math.Max(CpSat.MinTimeLimitSeconds, TimeLimit - stopwatch.Elapsed.TotalSeconds);

        /// <summary>One solve of whatever objective the model carries.</summary>
        public Outcome Single(Stage1Model built, double seconds)
        {
            using var solver = new CpSolver();
            var status = Run(solver, built.Model, seconds);
            return new Outcome(CpSat.HasSolution(status) ? Extract(built, solver) : null, status == CpSolverStatus.Optimal, status);
        }

        /// <summary>CostThenSoft of 04 §4: minimize cost, then the soft penalty at that cost (60 % / 40 % of the time).</summary>
        public Outcome Lexicographic(Stage1Model built, double seconds)
        {
            var start = stopwatch.Elapsed.TotalSeconds;
            using var solver = new CpSolver();
            built.Model.Minimize(built.Cost);
            var status = Run(solver, built.Model, seconds * SolverDefaults.Stage1CostPhaseShare);
            if (!CpSat.HasSolution(status))
            {
                return new Outcome(null, false, status);
            }

            var phaseOne = Extract(built, solver);
            var optimal = status == CpSolverStatus.Optimal;
            if (!built.HasSoftPenalty || cancellationToken.IsCancellationRequested)
            {
                return new Outcome(phaseOne, optimal, status);
            }

            var bestCost = (long)Math.Round(solver.ObjectiveValue);
            built.Model.Add(built.Cost <= bestCost);
            built.AddHintFromSolution(solver);
            built.Model.Minimize(built.SoftPenalty);
            var used = stopwatch.Elapsed.TotalSeconds - start;
            var remaining = Math.Max(seconds * (1 - SolverDefaults.Stage1CostPhaseShare), seconds - used);
            var phaseTwo = Run(solver, built.Model, remaining);
            return CpSat.HasSolution(phaseTwo)
                ? new Outcome(Extract(built, solver), optimal && phaseTwo == CpSolverStatus.Optimal, phaseTwo)
                : new Outcome(phaseOne, false, CpSolverStatus.Feasible);
        }

        public Stage1Result FinishLexicographic(Stage1Model built)
            => Finish(built, Lexicographic(built, Remaining));

        public Stage1Result Finish(Stage1Model built, Outcome outcome)
        {
            var cancelled = cancellationToken.IsCancellationRequested;
            var status = outcome.Solution is null
                ? CpSat.Map(outcome.Status, cancelled)
                : CpSat.Map(outcome.Optimal ? CpSolverStatus.Optimal : CpSolverStatus.Feasible, cancelled);
            var stats = new SolveStats
            {
                Stage1Ms = stopwatch.ElapsedMilliseconds,
                Classes = request.Classes.Count,
                Variables = built.Model.Model.Variables.Count,
                Constraints = built.Model.Model.Constraints.Count,
            };
            return new Stage1Result(status, outcome.Solution?.Counts ?? [], outcome.Solution?.Objective, [], stats);
        }

        private CpSolverStatus Run(CpSolver solver, CpModel model, double seconds)
        {
            solver.StringParameters = CpSatParameters.Build(Workers, seconds, request.Input.Options.RandomSeed);
            return CpSat.Solve(solver, model, callback, cancellationToken);
        }

        private Solution Extract(Stage1Model model, CpSolver solver)
        {
            var counts = new List<RoomClassCount>();
            foreach (var ((classIndex, roomIndex), variable) in model.Counts.OrderBy(c => c.Key.RoomIndex).ThenBy(c => c.Key.ClassIndex))
            {
                var value = (int)solver.Value(variable);
                if (value > 0)
                {
                    counts.Add(new RoomClassCount(classIndex, context.Rooms[roomIndex].Room.RoomId, value));
                }
            }

            var openRooms = context.Rooms.Where(r => solver.BooleanValue(model.Open[r.Index])).ToList();
            var penaltyByRule = model.PenaltyByRule.ToDictionary(e => e.Key, e => solver.Value(e.Value));
            var objective = new ObjectiveBreakdown(openRooms.Count, openRooms.Sum(r => r.Room.Cost), penaltyByRule.Values.Sum(), penaltyByRule);
            return new Solution(counts, objective);
        }
    }
}