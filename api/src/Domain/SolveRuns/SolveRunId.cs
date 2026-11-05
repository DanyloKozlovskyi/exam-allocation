namespace Domain.SolveRuns;

public record SolveRunId(Guid Value)
{
    public static SolveRunId Empty() => new(Guid.Empty);

    public static SolveRunId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}