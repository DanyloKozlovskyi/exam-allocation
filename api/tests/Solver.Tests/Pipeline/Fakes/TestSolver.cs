using Solver.Allocation;
using Solver.Alternatives;
using Solver.Model;
using Solver.Pipeline;
using Solver.Seating;
using Solver.Validation;

namespace Solver.Tests.Pipeline.Fakes;

/// <summary>Wires the real WS-A components with WS-B/WS-V test doubles.</summary>
public static class TestSolver
{
    public static Stage1Solver Stage1() => new(new FakeSeatGenerator(), new FakeIndependentSetCalculator());

    public static ConflictExplainer Explainer() => new(new FakeSeatGenerator(), new FakeIndependentSetCalculator());

    public static Solver.Precheck.Precheck Precheck() => new(new FakeSeatGenerator(), new FakeAdjacencyBuilder(), new FakeIndependentSetCalculator());

    public static AlternativesGenerator Alternatives(ISeatingSolver? seating = null)
        => new(Stage1(), seating ?? new FakeSeatingSolver(), new FakeExpander());

    public static SolverPipeline Pipeline(
        ISeatingSolver? seating = null,
        IAllocationValidator? validator = null,
        IStage1Solver? stage1 = null)
    {
        var seatingSolver = seating ?? new FakeSeatingSolver();
        return new SolverPipeline(
            Precheck(),
            new ClassBuilder(),
            stage1 ?? Stage1(),
            Explainer(),
            seatingSolver,
            new FakeExpander(),
            validator ?? new FakeValidator(),
            Alternatives(seatingSolver));
    }

    public static Stage1Request Request(SolverInput input, IReadOnlyList<Stage1Cut>? cuts = null, IReadOnlyList<IReadOnlyList<RoomClassCount>>? previous = null, long? costBound = null)
        => new(input, new ClassBuilder().Build(input), cuts ?? [], previous ?? [], costBound, null);
}

/// <summary>Synchronous progress sink (System.Progress posts asynchronously).</summary>
public sealed class RecordingProgress(Action<SolveProgress>? onReport = null) : IProgress<SolveProgress>
{
    private readonly List<SolveProgress> reports = [];

    public IReadOnlyList<SolveProgress> Reports
    {
        get
        {
            lock (reports)
            {
                return reports.ToList();
            }
        }
    }

    public void Report(SolveProgress value)
    {
        lock (reports)
        {
            reports.Add(value);
        }

        onReport?.Invoke(value);
    }
}

/// <summary>Stage-1 spy recording every request.</summary>
public sealed class RecordingStage1Solver(IStage1Solver inner) : IStage1Solver
{
    public List<Stage1Request> Requests { get; } = [];

    public Stage1Result Solve(Stage1Request request, IProgress<SolveProgress> progress, CancellationToken cancellationToken)
    {
        lock (Requests)
        {
            Requests.Add(request);
        }

        return inner.Solve(request, progress, cancellationToken);
    }
}