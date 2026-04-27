namespace LiveSessionService.Application.Features.Results;

/// <summary>
/// Paged result for list queries
/// S?n s�ng cho scale v?i pagination
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }

    public PagedResult() { }

    public PagedResult(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedResult<T> Create(
        List<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// For simple lists without pagination (backwards compatible)
    /// </summary>
    public static PagedResult<T> CreateUnpaged(List<T> items)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = items.Count,
            PageNumber = 1,
            PageSize = items.Count
        };
    }
}
