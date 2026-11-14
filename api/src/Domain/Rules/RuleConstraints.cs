namespace Domain.Rules;

public static class RuleConstraints
{
    public const int DisplayNameMaxLength = 200;
    public const int MinWeight = 1;
    public const int MaxWeight = 1000;
    public const int DefaultWeight = 10;
    public const int MinMaxPerScope = 1;
    public const int MaxMaxPerScope = 10000;
    public const int DefaultMaxPerScope = 1;
    public const double MinFillRatio = 0;
    public const double MaxFillRatio = 1;
    public const double DefaultMinFillRatio = 0.6;

    private static readonly HashSet<RuleType> HardOnly = [RuleType.AccessibleSeating];

    private static readonly HashSet<RuleType> SoftOnly =
    [
        RuleType.MinRoomFill,
        RuleType.KeepTogetherInCenter,
        RuleType.SpreadInRoom,
        RuleType.FillFromFront,
    ];

    /// <summary>Whether a rule of <paramref name="type"/> may be hard (docs/04 §2).</summary>
    public static bool AllowsHard(RuleType type) => Enum.IsDefined(type) && !SoftOnly.Contains(type);

    /// <summary>Whether a rule of <paramref name="type"/> may be soft (docs/04 §2).</summary>
    public static bool AllowsSoft(RuleType type) => Enum.IsDefined(type) && !HardOnly.Contains(type);

    public static bool Allows(RuleType type, bool isHard) => isHard ? AllowsHard(type) : AllowsSoft(type);
}