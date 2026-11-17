using System.Diagnostics;
using FluentAssertions;
using Solver.Allocation;
using Solver.Alternatives;
using Solver.Model;
using Solver.Pipeline;
using Solver.Seating;
using Solver.Tests.Pipeline;
using Solver.Tests.Pipeline.Fakes;
using Solver.Validation;
using Xunit;

namespace Solver.Tests.Alternatives;

public class AlternativesGeneratorTests
{
    [Fact]
    public async Task ShouldReturnDistinctValidAlternativesWithEnoughMovedParticipants()
    {
        // Arrange — docs/08: N = 3.
        var input = SolverPipelineTests.SmallInput() with { };
        input = input with { Options = input.Options with { Alternatives = 3, CostSlack = 0.5 } };
        var bestCostBound = 0m;

        // Act
        var output = await TestSolver.Pipeline().RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Status.Should().BeOneOf(SolveStatus.Optimal, SolveStatus.Feasible);
        output.Allocations.Should().HaveCount(4);
        output.Allocations.Select(a => a.Index).Should().Equal(0, 1, 2, 3);
        bestCostBound = output.Allocations[0].Objective.Cost * 1.5m;
        var validator = new FakeValidator();
        output.Allocations.Should().OnlyContain(a => validator.Validate(input, a.Items).IsValid);
        output.Allocations.Should().OnlyContain(a => a.Objective.Cost <= bestCostBound);
        var minimumMoved = (int)Math.Ceiling(input.Options.DiversityRatio * input.Participants.Count);
        for (var i = 0; i < output.Allocations.Count; i++)
        {
            for (var j = i + 1; j < output.Allocations.Count; j++)
            {
                var a = Placement(output.Allocations[i]);
                var b = Placement(output.Allocations[j]);
                var movedRooms = a.Count(e => b[e.Key].RoomId != e.Value.RoomId);
                var seatDistinct = a.Any(e => b[e.Key] != e.Value);
                (movedRooms >= minimumMoved || seatDistinct).Should().BeTrue();
                seatDistinct.Should().BeTrue("alternatives must be distinct");
            }
        }
    }

    [Fact]
    public async Task ShouldReportOnlyAlternativesProgressOnceAlternativesStart()
    {
        // Arrange — nested stage 1/2 solves of each alternative must not move clients' stage back.
        var input = SolverPipelineTests.SmallInput();
        input = input with { Options = input.Options with { Alternatives = 2, CostSlack = 0.5 } };
        var progress = new RecordingProgress();

        // Act
        await TestSolver.Pipeline().RunAsync(input, progress, CancellationToken.None);

        // Assert
        var stages = progress.Reports.Select(p => p.Stage).ToList();
        var start = stages.IndexOf(SolveStage.Alternatives);
        start.Should().BeGreaterThan(0);
        stages.Skip(start).Should().OnlyContain(s => s == SolveStage.Alternatives || s == SolveStage.Done);
        progress.Reports.Last(p => p.Stage == SolveStage.Alternatives).AlternativesDone.Should().Be(2);
    }

    [Fact]
    public async Task ShouldSwitchToSeatLevelWhenTheStage1AlternativeRunsOutOfTime()
    {
        // Arrange — review P3-2: stage 1 finds no diverse room assignment in time, but budget is left
        var input = SolverPipelineTests.SmallInput();
        input = input with { Options = input.Options with { Alternatives = 2, CostSlack = 0.5 } };
        var seating = new FakeSeatingSolver();
        var timingOut = new TimingOutAlternativeStage1(TestSolver.Stage1());
        var pipeline = new SolverPipeline(
            TestSolver.Precheck(),
            new ClassBuilder(),
            TestSolver.Stage1(),
            TestSolver.Explainer(),
            seating,
            new FakeExpander(),
            new FakeValidator(),
            new AlternativesGenerator(timingOut, seating, new FakeExpander()));

        // Act
        var output = await pipeline.RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert — seat-level alternatives instead of "time budget exhausted"
        timingOut.TimedOut.Should().BeGreaterThan(0);
        output.Allocations.Should().HaveCount(3);
        output.Diagnostics.Should().NotContain(d => d.Code == DiagnosticCode.AlternativesLimited);
        var validator = new FakeValidator();
        output.Allocations.Should().OnlyContain(a => validator.Validate(input, a.Items).IsValid);
    }

    [Fact]
    public async Task ShouldFallBackToSeatLevelDiversityWhenRoomsAreForced()
    {
        // Arrange — one room exactly full: stage-1 diversity is impossible.
        var input = new TestInputBuilder()
            .WithRoom(2, 3, 1)
            .WithGroup("A", 3).WithGroup("B", 3)
            .WithRule(RuleType.SeparateByAttribute, new SeparateByAttributeParameters(TestInputBuilder.GroupKey, RuleScope.Room, 3))
            .WithOptions(o => o with { Alternatives = 2 })
            .Build();

        // Act
        var output = await TestSolver.Pipeline().RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Allocations.Should().HaveCount(3);
        output.Allocations.Should().OnlyContain(a => a.Items.All(i => i.RoomId == input.Rooms[0].RoomId));
        output.Allocations.Select(a => string.Join(";", a.Items.OrderBy(i => i.ParticipantId).Select(i => i.SeatKey))).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task ShouldReportAlternativesLimitedWhenNoDistinctAllocationExists()
    {
        // Arrange — a single participant on a single seat.
        var input = new TestInputBuilder().WithRoom(1, 1, 1).WithGroup("A", 1).WithOptions(o => o with { Alternatives = 2 }).Build();

        // Act
        var output = await TestSolver.Pipeline().RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Allocations.Should().ContainSingle();
        var diagnostic = output.Diagnostics.Should().ContainSingle(d => d.Code == DiagnosticCode.AlternativesLimited).Subject;
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.Arguments[DiagnosticArgument.Reason].Should().Be(nameof(Solver.Alternatives.AlternativesLimitReason.NoDistinctAllocation));
        Solver.Resources.DiagnosticMessages.Render(diagnostic).Should().NotContain("{").And.NotContain(nameof(Solver.Alternatives.AlternativesLimitReason.NoDistinctAllocation));
    }

    [Fact]
    public async Task ShouldStartStage1AlternativesWithTheFeedbackCutsOfTheBestAllocation()
    {
        // Arrange — R-20: the first stage-2 call fails, so the best allocation needs a feedback cut.
        var input = new TestInputBuilder()
            .WithRoom(2, 3, 1).WithRoom(2, 3, 1).WithRoom(2, 3, 1)
            .WithGroup("A", 4).WithGroup("B", 4)
            .WithRule(RuleType.NoAdjacentByAttribute, new NoAdjacentByAttributeParameters(TestInputBuilder.GroupKey, [AdjacencyKind.Side], false))
            .WithOptions(o => o with { Alternatives = 2, CostSlack = 1 })
            .Build();
        var seating = new FailingSeatingSolver(new FakeSeatingSolver(), failures: 1);
        var pipelineStage1 = new RecordingStage1Solver(TestSolver.Stage1());
        var alternativesStage1 = new RecordingStage1Solver(TestSolver.Stage1());
        var pipeline = new SolverPipeline(
            TestSolver.Precheck(),
            new ClassBuilder(),
            pipelineStage1,
            TestSolver.Explainer(),
            seating,
            new FakeExpander(),
            new FakeValidator(),
            new AlternativesGenerator(alternativesStage1, seating, new FakeExpander()));

        // Act
        var output = await pipeline.RunAsync(input, new RecordingProgress(), CancellationToken.None);

        // Assert
        output.Stats.FeedbackIterations.Should().BeGreaterThanOrEqualTo(1);
        var learned = pipelineStage1.Requests[^1].Cuts;
        learned.Should().NotBeEmpty();
        alternativesStage1.Requests.Should().NotBeEmpty();
        alternativesStage1.Requests.Should().OnlyContain(r => learned.All(cut => r.Cuts.Contains(cut)));
        var validator = new AllocationValidator();
        output.Allocations.Should().HaveCountGreaterThan(1);
        output.Allocations.Should().OnlyContain(a => validator.Validate(input, a.Items).IsValid);
    }

    [Fact]
    public async Task ShouldKeepStage2OfAlternativesWithinTheTimeBudget()
    {
        // Arrange — R-20: budget = 2 × 1 s; stage 2 of every alternative blocks until it is cancelled.
        var input = SolverPipelineTests.SmallInput();
        input = input with { Options = input.Options with { Alternatives = 3, CostSlack = 0.5, Stage1TimeLimitSeconds = 1, Stage2TimeLimitSecondsPerRoom = 30 } };
        var best = await TestSolver.Pipeline().RunAsync(input with { Options = input.Options with { Alternatives = 0 } }, new RecordingProgress(), CancellationToken.None);
        var classes = new ClassBuilder().Build(input);
        var stage1 = TestSolver.Stage1().Solve(TestSolver.Request(input), new RecordingProgress(), CancellationToken.None);
        var seating = new BlockingSeatingSolver();
        var generator = TestSolver.Alternatives(seating);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = generator.Generate(new AlternativesRequest(input, classes, stage1, best.Allocations[0]), new RecordingProgress(), CancellationToken.None);

        // Assert
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
        result.Allocations.Should().BeEmpty();
        var diagnostic = result.Diagnostics.Should().ContainSingle(d => d.Code == DiagnosticCode.AlternativesLimited).Subject;
        diagnostic.Arguments[DiagnosticArgument.Reason].Should().Be(nameof(AlternativesLimitReason.TimeBudgetExhausted));
        seating.TimeLimits.Should().NotBeEmpty();
        seating.TimeLimits.Should().OnlyContain(t => t <= 2 * input.Options.Stage1TimeLimitSeconds);
    }

    [Fact]
    public async Task ShouldReportCancelledWhenTheCallerCancelsDuringStage2()
    {
        // Arrange — the caller's token (not the budget) stops a blocked stage 2.
        var input = SolverPipelineTests.SmallInput();
        input = input with { Options = input.Options with { Alternatives = 2, CostSlack = 0.5, Stage1TimeLimitSeconds = 30 } };
        var best = await TestSolver.Pipeline().RunAsync(input with { Options = input.Options with { Alternatives = 0 } }, new RecordingProgress(), CancellationToken.None);
        var classes = new ClassBuilder().Build(input);
        var stage1 = TestSolver.Stage1().Solve(TestSolver.Request(input), new RecordingProgress(), CancellationToken.None);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        var result = TestSolver.Alternatives(new BlockingSeatingSolver())
            .Generate(new AlternativesRequest(input, classes, stage1, best.Allocations[0]), new RecordingProgress(), cancellation.Token);

        // Assert
        result.Allocations.Should().BeEmpty();
        result.Diagnostics.Should().ContainSingle(d => d.Code == DiagnosticCode.AlternativesLimited)
            .Which.Arguments[DiagnosticArgument.Reason].Should().Be(nameof(AlternativesLimitReason.Cancelled));
    }

    private static Dictionary<Guid, (Guid RoomId, string SeatKey)> Placement(SolvedAllocation allocation)
        => allocation.Items.ToDictionary(i => i.ParticipantId, i => (i.RoomId, i.SeatKey));

    /// <summary>Stage-2 double that blocks until its token is cancelled and records the per-room time limits it was given.</summary>
    private sealed class BlockingSeatingSolver : ISeatingSolver
    {
        private readonly List<int> timeLimits = [];

        public IReadOnlyList<int> TimeLimits
        {
            get
            {
                lock (timeLimits)
                {
                    return timeLimits.ToList();
                }
            }
        }

        public SeatingResult Solve(SeatingRequest request, CancellationToken cancellationToken)
        {
            lock (timeLimits)
            {
                timeLimits.Add(request.TimeLimitSeconds);
            }

            cancellationToken.WaitHandle.WaitOne();
            return new SeatingResult(SolveStatus.Cancelled, [], 0, new Dictionary<Guid, long>());
        }
    }
}

/// <summary>Stage 1 that reports NoSolutionInTime for every alternative (requests with previous solutions).</summary>
internal sealed class TimingOutAlternativeStage1(IStage1Solver inner) : IStage1Solver
{
    public int TimedOut { get; private set; }

    public Stage1Result Solve(Stage1Request request, IProgress<SolveProgress> progress, CancellationToken cancellationToken)
    {
        if (request.PreviousSolutions.Count == 0)
        {
            return inner.Solve(request, progress, cancellationToken);
        }

        TimedOut++;
        return new Stage1Result(SolveStatus.NoSolutionInTime, [], null, [], new SolveStats());
    }
}