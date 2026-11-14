namespace Tools.Shared.SyntheticData;

public enum DatasetSize
{
    Small = 1,
    Medium = 2,
    Large = 3,
    Custom = 4,
}

/// <summary>Parameters of a synthetic dataset; presets follow the scale table of docs/01.</summary>
public sealed record GeneratorSettings(
    DatasetSize Size,
    int Participants,
    int Centers,
    int RoomsPerCenter,
    int MinGroupSize,
    int MaxGroupSize,
    double NeedsRatio,
    int Seed)
{
    public const int DefaultMinGroupSize = 25;
    public const int DefaultMaxGroupSize = 32;
    public const double DefaultNeedsRatio = 0.02;
    public const int DefaultSeed = 1;

    private const int SmallParticipants = 300;
    private const int SmallCenters = 2;
    private const int SmallRoomsPerCenter = 6;
    private const int MediumParticipants = 2_000;
    private const int MediumCenters = 10;
    private const int MediumRoomsPerCenter = 8;
    private const int LargeParticipants = 10_000;
    private const int LargeCenters = 50;
    private const int LargeRoomsPerCenter = 8;

    public static GeneratorSettings ForSize(DatasetSize size) => size switch
    {
        DatasetSize.Small => Preset(size, SmallParticipants, SmallCenters, SmallRoomsPerCenter),
        DatasetSize.Medium => Preset(size, MediumParticipants, MediumCenters, MediumRoomsPerCenter),
        DatasetSize.Large => Preset(size, LargeParticipants, LargeCenters, LargeRoomsPerCenter),

        // Custom starts from the small preset; every value is expected to be overridden by flags.
        DatasetSize.Custom => Preset(size, SmallParticipants, SmallCenters, SmallRoomsPerCenter),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    public string FolderName => Size.ToString().ToLowerInvariant();

    private static GeneratorSettings Preset(DatasetSize size, int participants, int centers, int roomsPerCenter)
        => new(size, participants, centers, roomsPerCenter, DefaultMinGroupSize, DefaultMaxGroupSize, DefaultNeedsRatio, DefaultSeed);
}