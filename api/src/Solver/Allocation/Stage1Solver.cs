using System.Diagnostics;
using Google.OrTools.Sat;
using Solver.Layout;
using Solver.Model;
using Solver.Precheck;

namespace Solver.Allocation;

/// <summary>Stage 1: participant classes to rooms (04 §4).</summary>
public sealed class Stage1Solver(ISeatGenerator seatGenerator, IIndependentSetCalculator independentSetCalculator) : IStage1Solver
{
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

        var built = Stage1ModelBuilder.Build(context, request, explain: false, cancellationToken);
        var timeLimit = (double)options.Stage1TimeLimitSeconds;
        var workers = CpSat.Workers(options.NumWorkers);
        using var solver = new CpSolver();
        var callback = new ThrottledProgressCallback(progress, SolveStage.Stage1);

        CpSolverStatus status;
        var optimal = true;
        if (request.PreviousSolutions.Count > 0)
        {
            built.Model.Minimize(built.SoftPenalty);
            status = Run(solver, built.Model, workers, timeLimit, options.RandomSeed, callback, cancellationToken);
            optimal = status == CpSolverStatus.Optimal;
        }
        else if (options.ObjectiveMode == ObjectiveMode.Weighted)
        {
            built.Model.Minimize((SolverDefaults.WeightedCostMultiplier * built.Cost) + built.SoftPenalty);
            status = Run(solver, built.Model, workers, timeLimit, options.RandomSeed, callback, cancellationToken);
            optimal = status == CpSolverStatus.Optimal;
        }
        else
        {
            built.Model.Minimize(built.Cost);
            status = Run(solver, built.Model, workers, timeLimit * SolverDefaults.Stage1CostPhaseShare, options.RandomSeed, callback, cancellationToken);
            optimal = status == CpSolverStatus.Optimal;
            if (CpSat.HasSolution(status) && built.HasSoftPenalty && !cancellationToken.IsCancellationRequested)
            {
                var phaseOne = Extract(context, built, solver);
                var bestCost = (long)Math.Round(solver.ObjectiveValue);
                built.Model.Add(built.Cost <= bestCost);
                built.AddHintFromSolution(solver);
                built.Model.Minimize(built.SoftPenalty);
                var remaining = Math.Max(timeLimit * (1 - SolverDefaults.Stage1CostPhaseShare), timeLimit - stopwatch.Elapsed.TotalSeconds);
                var phaseTwo = Run(solver, built.Model, workers, remaining, options.RandomSeed, callback, cancellationToken);
                if (!CpSat.HasSolution(phaseTwo))
                {
                    return Result(CpSat.Map(CpSolverStatus.Feasible, cancellationToken.IsCancellationRequested), phaseOne, built, request, stopwatch);
                }

                status = phaseTwo;
                optimal &= phaseTwo == CpSolverStatus.Optimal;
            }
        }

        var cancelled = cancellationToken.IsCancellationRequested;
        if (!CpSat.HasSolution(status))
        {
            return Result(CpSat.Map(status, cancelled), null, built, request, stopwatch);
        }

        var finalStatus = CpSat.Map(optimal ? CpSolverStatus.Optimal : CpSolverStatus.Feasible, cancelled);
        return Result(finalStatus, Extract(context, built, solver), built, request, stopwatch);
    }

    private static CpSolverStatus Run(CpSolver solver, CpModel model, int workers, double seconds, int seed, SolutionCallback callback, CancellationToken cancellationToken)
    {
        solver.StringParameters = CpSat.Parameters(workers, seconds, seed);
        return CpSat.Solve(solver, model, callback, cancellationToken);
    }

    private static (IReadOnlyList<RoomClassCount> Counts, ObjectiveBreakdown Objective) Extract(Stage1Context context, Stage1Model model, CpSolver solver)
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
        return (counts, objective);
    }

    private static Stage1Result Result(
        SolveStatus status,
        (IReadOnlyList<RoomClassCount> Counts, ObjectiveBreakdown Objective)? solution,
        Stage1Model model,
        Stage1Request request,
        Stopwatch stopwatch)
    {
        var stats = new SolveStats
        {
            Stage1Ms = stopwatch.ElapsedMilliseconds,
            Classes = request.Classes.Count,
            Variables = model.Model.Model.Variables.Count,
            Constraints = model.Model.Model.Constraints.Count,
        };
        return new Stage1Result(status, solution?.Counts ?? [], solution?.Objective, [], stats);
    }
}