namespace Infrastructure.Caching;

public static class CacheKeys
{
    private const string ImportPrefix = "import:";

    public static string Import(Guid importId) => $"{ImportPrefix}{importId:N}";
}