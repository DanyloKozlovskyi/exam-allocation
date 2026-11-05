namespace Domain.Allocations;

public record AllocationId(Guid Value)
{
    public static AllocationId Empty() => new(Guid.Empty);

    public static AllocationId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}