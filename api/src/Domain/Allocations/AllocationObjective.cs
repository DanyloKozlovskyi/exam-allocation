namespace Domain.Allocations;

public sealed record AllocationObjective
{
    public int RoomsUsed { get; init; }

    public decimal Cost { get; init; }

    public long SoftPenalty { get; init; }

    public IReadOnlyDictionary<Guid, long> PenaltyByRule { get; init; } = new Dictionary<Guid, long>();
}