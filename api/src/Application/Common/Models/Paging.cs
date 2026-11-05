namespace Application.Common.Models;

public enum SortDirection
{
    Ascending = 1,
    Descending = 2,
}

public sealed record PageRequest(int Page, int PageSize, string? Search, SortDirection SortDirection)
{
    public const int FirstPage = 1;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 500;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);