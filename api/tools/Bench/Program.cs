using System.Diagnostics;
using System.Globalization;
using Bench;
using Microsoft.Extensions.DependencyInjection;
using Solver;
using Solver.Model;
using Solver.Pipeline;
using Solver.Validation;
using Tools.Shared.CommandLine;
using Tools.Shared.Json;
using Tools.Shared.SyntheticData;

const int DefaultRepeat = 3;
const int DefaultAlternatives = 0;
const double TargetToleranceFactor = 1.5;
const string Usage = "Usage: Bench --size small|medium|large [--alternatives 0] [--repeat 3] [--workers 0] [--seed 1] [--input <solver-input.json>]";

BenchSettings settings;
SolverInput baseInput;
try
{
    var arguments = CommandLineArguments.Parse(args);
    var size = arguments.GetEnum<DatasetSize>(ArgumentNames.Size);
    var inputPath = arguments.GetString(ArgumentNames.Input);
    if (size == DatasetSize.Custom || (size is null && inputPath is null))
    {
        throw new ArgumentException($"{CommandLineArguments.Prefix}{ArgumentNames.Size} small|medium|large or {CommandLineArguments.Prefix}{ArgumentNames.Input} is required");
    }

    settings = new BenchSettings(
        size,
        inputPath,
        arguments.GetInt(ArgumentNames.Alternatives) ?? DefaultAlternatives,
        Math.Max(arguments.GetInt(ArgumentNames.Repeat) ?? DefaultRepeat, 1),
        arguments.GetInt(ArgumentNames.Workers),
        arguments.GetInt(ArgumentNames.Seed) ?? GeneratorSettings.DefaultSeed);

    baseInput = inputPath is not null
        ? SolverJson.Deserialize<SolverInput>(File.ReadAllText(inputPath))
        : SyntheticDataGenerator.Generate(GeneratorSettings.ForSize(size!.Value) with { Seed = settings.Seed }).SolverInput;
}
catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException or IOException or NotSupportedException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(Usage);
    return (int)BenchExitCode.Usage;
}

var input = baseInput with
{
    Options = baseInput.Options with
    {
        Alternatives = settings.Alternatives,
        NumWorkers = settings.Workers ?? baseInput.Options.NumWorkers,
    },
};

var target = settings.Size is { } targetSize && settings.Alternatives == 0 ? PerformanceTargets.FullRun(targetSize) : null;
var seats = input.Rooms.Sum(x => (x.Layout.Rows * x.Layout.Columns * x.Layout.SeatsPerDesk) - x.Layout.DisabledSeats.Count);

Console.WriteLine($"# Bench — {settings.Size?.ToString() ?? Path.GetFileName(settings.InputPath)}");
Console.WriteLine();
Console.WriteLine(
    $"Participants: {input.Participants.Count} · Rooms: {input.Rooms.Count} · Seats: {seats} · Rules: {input.Rules.Count} · "
    + $"Alternatives: {input.Options.Alternatives} · Workers: {input.Options.NumWorkers} · Repeat: {settings.Repeat} · "
    + $"Target: {(target is { } t ? $"{t.TotalSeconds:0} s (fail > {t.TotalSeconds * TargetToleranceFactor:0.#} s)" : "n/a")}");
Console.WriteLine();

var pipeline = new MinimalServiceProvider(new ServiceCollection().AddSolver()).GetRequiredService<ISolverPipeline>();
var validator = new AllocationValidator();
var rows = new List<BenchRow>();
var exitCode = BenchExitCode.Success;

for (var run = 1; run <= settings.Repeat; run++)
{
    var stopwatch = Stopwatch.StartNew();
    SolverOutput output;
    try
    {
        output = await pipeline.RunAsync(input, new Progress<SolveProgress>(), CancellationToken.None);
    }
    catch (Exception ex) when (Unwrap(ex) is NotImplementedException notImplemented)
    {
        var origin = notImplemented.TargetSite is { } site ? $"{site.DeclaringType?.FullName}.{site.Name}" : "unknown";
        Console.Error.WriteLine($"Solver pipeline is not implemented yet (NotImplementedException in {origin}). Nothing to benchmark.");
        return (int)BenchExitCode.NotImplemented;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Solver pipeline failed: {Unwrap(ex)}");
        return (int)BenchExitCode.SolverError;
    }

    stopwatch.Stop();
    var reports = output.Allocations.Select(x => validator.Validate(input, x.Items)).ToList();
    var row = new BenchRow(run, output, stopwatch.Elapsed, reports);
    rows.Add(row);

    if (!row.Solved)
    {
        exitCode = Worst(exitCode, BenchExitCode.NotSolved);
    }
    else if (!row.AllValid)
    {
        exitCode = Worst(exitCode, BenchExitCode.ValidationFailed);
    }

    if (target is { } limit && row.Elapsed > limit * TargetToleranceFactor)
    {
        exitCode = Worst(exitCode, BenchExitCode.TimeOverTarget);
    }
}

MarkdownReport.Print(rows);
if (rows.SelectMany(x => x.Reports.SelectMany(r => r.Violations.Where(v => v.IsHard))).Take(MarkdownReport.MaxListedViolations).ToList() is { Count: > 0 } hard)
{
    Console.WriteLine();
    Console.WriteLine("Hard violations (first):");
    hard.ForEach(x => Console.WriteLine($"- {x.Kind} rule={x.RuleId} participant={x.ParticipantId} room={x.RoomId} seat={x.SeatKey}"));
}

Console.WriteLine();
Console.WriteLine($"Result: {exitCode}");
return (int)exitCode;

static Exception Unwrap(Exception ex)
{
    while (ex is AggregateException { InnerException: { } inner })
    {
        ex = inner;
    }

    return ex;
}

static BenchExitCode Worst(BenchExitCode current, BenchExitCode candidate) => current == BenchExitCode.Success ? candidate : current;

internal enum BenchExitCode
{
    Success = 0,
    ValidationFailed = 1,
    NotImplemented = 2,
    TimeOverTarget = 3,
    NotSolved = 4,
    SolverError = 5,
    Usage = 64,
}

internal static class ArgumentNames
{
    public const string Size = "size";
    public const string Alternatives = "alternatives";
    public const string Repeat = "repeat";
    public const string Workers = "workers";
    public const string Seed = "seed";
    public const string Input = "input";
}

internal sealed record BenchSettings(DatasetSize? Size, string? InputPath, int Alternatives, int Repeat, int? Workers, int Seed);

/// <summary>Full-run (best only) targets from docs/04 §12.</summary>
internal static class PerformanceTargets
{
    private const int SmallSeconds = 5;
    private const int MediumSeconds = 30;
    private const int LargeSeconds = 120;

    public static TimeSpan? FullRun(DatasetSize size) => size switch
    {
        DatasetSize.Small => TimeSpan.FromSeconds(SmallSeconds),
        DatasetSize.Medium => TimeSpan.FromSeconds(MediumSeconds),
        DatasetSize.Large => TimeSpan.FromSeconds(LargeSeconds),
        _ => null,
    };
}

internal sealed record BenchRow(int Run, SolverOutput Output, TimeSpan Elapsed, IReadOnlyList<ValidationReport> Reports)
{
    public bool Solved => Output.Status is SolveStatus.Optimal or SolveStatus.Feasible && Output.Allocations.Count > 0;

    public bool AllValid => Reports.All(x => x.IsValid);
}

internal static class MarkdownReport
{
    public const int MaxListedViolations = 10;

    private const string Separator = " | ";

    private static readonly string[] Columns =
    [
        "Run", "Status", "Total ms", "Precheck ms", "Stage 1 ms", "Stage 2 ms", "Feedback it.", "Alternatives ms",
        "Classes", "Vars", "Constraints", "Allocations", "Rooms used", "Cost", "Soft penalty", "Validator",
    ];

    public static void Print(IReadOnlyList<BenchRow> rows)
    {
        Console.WriteLine($"| {string.Join(Separator, Columns)} |");
        Console.WriteLine($"|{string.Concat(Enumerable.Repeat("---|", Columns.Length))}");
        foreach (var row in rows)
        {
            var stats = row.Output.Stats;
            var best = row.Output.Allocations.FirstOrDefault();
            string[] cells =
            [
                Number(row.Run),
                row.Output.Status.ToString(),
                Number((long)row.Elapsed.TotalMilliseconds),
                Number(stats.PrecheckMs),
                Number(stats.Stage1Ms),
                Number(stats.Stage2Ms),
                Number(stats.FeedbackIterations),
                Number(stats.AlternativesMs),
                Number(stats.Classes),
                Number(stats.Variables),
                Number(stats.Constraints),
                Number(row.Output.Allocations.Count),
                best is null ? "–" : Number(best.Objective.RoomsUsed),
                best is null ? "–" : best.Objective.Cost.ToString(CultureInfo.InvariantCulture),
                best is null ? "–" : Number(best.Objective.SoftPenalty),
                ValidatorCell(row),
            ];
            Console.WriteLine($"| {string.Join(Separator, cells)} |");
        }
    }

    private static string ValidatorCell(BenchRow row)
    {
        if (row.Reports.Count == 0)
        {
            return "no allocation";
        }

        var hard = row.Reports.Sum(x => x.Violations.Count(v => v.IsHard));
        var verdict = row.AllValid ? "valid" : $"INVALID ({hard} hard)";
        var best = row.Output.Allocations[0];
        var penalty = row.Reports[0].SoftPenalty;
        return penalty == best.Objective.SoftPenalty ? verdict : $"{verdict}; penalty {Number(penalty)} ≠ objective";
    }

    private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);
}