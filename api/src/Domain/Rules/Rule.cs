using Domain.Common;
using Domain.Rules.Parameters;
using Domain.Sessions;

namespace Domain.Rules;

public class Rule : IEntity<RuleId>, IVersioned
{
    private Rule(RuleId id, ExamSessionId sessionId, RuleParameters parameters, bool isHard, int weight, bool isEnabled, string? displayName)
    {
        Id = id;
        SessionId = sessionId;
        Type = parameters.RuleType;
        Parameters = parameters;
        IsHard = isHard;
        Weight = weight;
        IsEnabled = isEnabled;
        DisplayName = displayName;
    }

    private Rule()
    {
        Id = RuleId.Empty();
        SessionId = ExamSessionId.Empty();
        Parameters = new FillFromFrontParameters();
    }

    public RuleId Id { get; private set; }

    public ExamSessionId SessionId { get; private set; }

    public RuleType Type { get; private set; }

    public RuleParameters Parameters { get; private set; }

    public bool IsHard { get; private set; }

    public int Weight { get; private set; }

    public bool IsEnabled { get; private set; }

    public string? DisplayName { get; private set; }

    public uint Version { get; private set; }

    public static Rule New(RuleId id, ExamSessionId sessionId, RuleParameters parameters, bool isHard, int weight, bool isEnabled, string? displayName)
    {
        Validate(parameters, isHard, weight, displayName);
        return new Rule(id, sessionId, parameters, isHard, weight, isEnabled, Clean(displayName));
    }

    public void UpdateDetails(RuleParameters parameters, bool isHard, int weight, bool isEnabled, string? displayName)
    {
        Validate(parameters, isHard, weight, displayName);
        Type = parameters.RuleType;
        Parameters = parameters;
        IsHard = isHard;
        Weight = weight;
        IsEnabled = isEnabled;
        DisplayName = Clean(displayName);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Validate(RuleParameters parameters, bool isHard, int weight, string? displayName)
    {
        Guard.NotNull(parameters, nameof(parameters));
        Guard.IsTrue(RuleConstraints.Allows(parameters.RuleType, isHard), nameof(isHard), $"Rule type {parameters.RuleType} does not allow IsHard = {isHard}");
        Guard.InRange(weight, RuleConstraints.MinWeight, RuleConstraints.MaxWeight, nameof(weight));
        Guard.MaxLength(displayName, RuleConstraints.DisplayNameMaxLength, nameof(displayName));
    }
}