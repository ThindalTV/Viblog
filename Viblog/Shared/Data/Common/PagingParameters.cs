namespace Viblog.Shared.Data.Common;

/// <summary>
/// Parameters for pagination
/// </summary>
public class PagingParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    /// <summary>
    /// Page number (1-based). Defaults to 1.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page. Defaults to 10, maximum is 100.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    /// <summary>
    /// Number of items to skip based on current page
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Creates default paging parameters (page 1, size 10)
    /// </summary>
    public PagingParameters()
    {
    }

    /// <summary>
    /// Creates paging parameters with specific values
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    public PagingParameters(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber > 0 ? pageNumber : 1;
        PageSize = pageSize;
    }
}
