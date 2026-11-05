using Application.Common.Models;

namespace Api.Dtos;

public record PagedDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public static PagedDto<T> From<TSource>(PagedResult<TSource> result, Func<TSource, T> map)
        => new(result.Items.Select(map).ToList(), result.Page, result.PageSize, result.Total);
}