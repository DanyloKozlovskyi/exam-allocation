namespace Domain.SolveRuns;

public sealed record SolveRunStats
{
    public long PrecheckMs { get; init; }

    public long Stage1Ms { get; init; }

    public long Stage2Ms { get; init; }

    public long AlternativesMs { get; init; }

    public int FeedbackIterations { get; init; }

    public int Classes { get; init; }

    public int Variables { get; init; }

    public int Constraints { get; init; }
}