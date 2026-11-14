using DataGen;
using Solver.Model;
using Tools.Shared.CommandLine;
using Tools.Shared.Json;
using Tools.Shared.SyntheticData;

const int SuccessExitCode = 0;
const int RoundTripFailedExitCode = 1;
const int UsageExitCode = 64;
const string RangeSeparator = "..";
const string DefaultOutputDirectory = "samples/generated";
const string Usage = "Usage: DataGen --size small|medium|large|custom [--participants N --centers C --rooms-per-center R "
    + "--group-size 25..32 --needs-ratio 0.02] --seed 1 --out <dir>";

GeneratorSettings settings;
string outputRoot;
try
{
    var arguments = CommandLineArguments.Parse(args);
    var size = arguments.GetEnum<DatasetSize>(ArgumentNames.Size) ?? DatasetSize.Small;
    var preset = GeneratorSettings.ForSize(size);
    var groupSize = arguments.GetRange(ArgumentNames.GroupSize, RangeSeparator) ?? (preset.MinGroupSize, preset.MaxGroupSize);
    settings = preset with
    {
        Participants = arguments.GetInt(ArgumentNames.Participants) ?? preset.Participants,
        Centers = arguments.GetInt(ArgumentNames.Centers) ?? preset.Centers,
        RoomsPerCenter = arguments.GetInt(ArgumentNames.RoomsPerCenter) ?? preset.RoomsPerCenter,
        MinGroupSize = groupSize.Min,
        MaxGroupSize = groupSize.Max,
        NeedsRatio = arguments.GetDouble(ArgumentNames.NeedsRatio) ?? preset.NeedsRatio,
        Seed = arguments.GetInt(ArgumentNames.Seed) ?? preset.Seed,
    };
    outputRoot = arguments.GetString(ArgumentNames.Out) ?? DefaultOutputDirectory;

    if (settings.Participants <= 0 || settings.Centers <= 0 || settings.RoomsPerCenter <= 0 || settings.MinGroupSize <= 0)
    {
        throw new ArgumentException("Participants, centers, rooms per center and group size must be positive");
    }
}
catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(Usage);
    return UsageExitCode;
}

var dataset = SyntheticDataGenerator.Generate(settings);
var directory = Path.Combine(outputRoot, settings.FolderName);
Directory.CreateDirectory(directory);

ParticipantsWorkbookWriter.Write(Path.Combine(directory, OutputFiles.ParticipantsXlsx), dataset.Participants);
WriteJson(OutputFiles.ParticipantsJson, dataset.Participants);
WriteJson(OutputFiles.CentersJson, dataset.Centers);
WriteJson(OutputFiles.RoomsJson, dataset.Rooms);
WriteJson(OutputFiles.RulesJson, dataset.Rules);
WriteJson(OutputFiles.SolverInputJson, dataset.SolverInput);

// Polymorphic rule parameters and sets must survive a round-trip (Bench and tests load this file).
var solverInputJson = File.ReadAllText(Path.Combine(directory, OutputFiles.SolverInputJson));
if (SolverJson.Serialize(SolverJson.Deserialize<SolverInput>(solverInputJson)) != solverInputJson)
{
    Console.Error.WriteLine($"{OutputFiles.SolverInputJson} does not round-trip through System.Text.Json");
    return RoundTripFailedExitCode;
}

Console.WriteLine(
    $"Generated {settings.FolderName} (seed {settings.Seed}): {dataset.Participants.Count} participants, "
    + $"{dataset.Participants.Select(x => x.Group).Distinct().Count()} groups, {dataset.Centers.Count} centers, "
    + $"{dataset.Rooms.Count} rooms ({dataset.Rooms.Sum(x => x.Capacity)} seats), {dataset.Rules.Count} rules → {Path.GetFullPath(directory)}");
return SuccessExitCode;

void WriteJson<T>(string fileName, T value)
    => File.WriteAllText(Path.Combine(directory, fileName), SolverJson.Serialize(value));

internal static class ArgumentNames
{
    public const string Size = "size";
    public const string Participants = "participants";
    public const string Centers = "centers";
    public const string RoomsPerCenter = "rooms-per-center";
    public const string GroupSize = "group-size";
    public const string NeedsRatio = "needs-ratio";
    public const string Seed = "seed";
    public const string Out = "out";
}

internal static class OutputFiles
{
    public const string ParticipantsXlsx = "participants.xlsx";
    public const string ParticipantsJson = "participants.json";
    public const string CentersJson = "centers.json";
    public const string RoomsJson = "rooms.json";
    public const string RulesJson = "rules.json";
    public const string SolverInputJson = "solver-input.json";
}