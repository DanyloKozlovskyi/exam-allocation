namespace Domain.Rules;

public record RuleId(Guid Value)
{
    public static RuleId Empty() => new(Guid.Empty);

    public static RuleId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}