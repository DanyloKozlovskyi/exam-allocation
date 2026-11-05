namespace Domain.Allocations;

public record AllocationItemId(Guid Value)
{
    public static AllocationItemId Empty() => new(Guid.Empty);

    public static AllocationItemId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}