namespace Application.Solving.Mapping;

/// <summary>Maps enums with identical member names across the Domain / Solver boundary.</summary>
public static class EnumMapper
{
    public static TTo Map<TFrom, TTo>(TFrom value)
        where TFrom : struct, Enum
        where TTo : struct, Enum
    {
        var name = value.ToString();
        if (!Enum.TryParse<TTo>(name, ignoreCase: false, out var result))
        {
            throw new ArgumentOutOfRangeException(nameof(value), name, $"No {typeof(TTo).Name} member named {name}");
        }

        return result;
    }
}