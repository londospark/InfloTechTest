namespace UserManagement.Shared.DTOs;

/// <summary>
/// Generic paged result wrapper containing items and paging metadata.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class PagedResultDto<T>
{
    /// <summary>
    /// Creates a new <see cref="PagedResultDto{T}"/> instance.
    /// </summary>
    /// <param name="items">The items for the current page.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    public PagedResultDto(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// The items for the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// The current 1-based page number.
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// The configured page size.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// The total number of items available across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// True when there are more pages available after this one.
    /// </summary>
    public bool HasMore => Page * PageSize < TotalCount;
}
