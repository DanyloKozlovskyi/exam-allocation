namespace Domain.Pins;

public record PinId(Guid Value)
{
    public static PinId Empty() => new(Guid.Empty);

    public static PinId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}