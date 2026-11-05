namespace Domain.Centers;

public record TestCenterId(Guid Value)
{
    public static TestCenterId Empty() => new(Guid.Empty);

    public static TestCenterId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}