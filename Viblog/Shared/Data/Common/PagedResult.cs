namespace Vilog.Shared.Data.Common;

/// <summary>
/// Base class containing pagination metadata without generic type
/// </summary>
public abstract class PagedResultBase
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Represents a paginated result set with metadata
/// </summary>
/// <typeparam name="T">The type of items in the result set</typeparam>
public class PagedResult<T> : PagedResultBase
{
    /// <summary>
    /// The items in the current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>
    /// Creates an empty paged result
    /// </summary>
    public PagedResult()
    {
    }

    /// <summary>
    /// Creates a paged result with items and metadata
    /// </summary>
    /// <param name="items">The items in the current page</param>
    /// <param name="totalCount">Total number of items across all pages</param>
    /// <param name="pageNumber">Current page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
