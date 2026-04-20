namespace Viblog.Admin.Components.DataGrid;

/// <summary>
/// Describes a single sort descriptor applied to the grid.
/// </summary>
public sealed class GridSortDescriptor
{
    /// <summary>
    /// The field/member name to sort by.
    /// </summary>
    public string Member { get; set; } = string.Empty;

    /// <summary>
    /// The direction of the sort.
    /// </summary>
    public SortDirection SortDirection { get; set; }
}

/// <summary>
/// Contains the request details from the grid (paging, sorting).
/// </summary>
public sealed class GridRequest
{
    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// The active sort descriptors.
    /// </summary>
    public IReadOnlyList<GridSortDescriptor> Sorts { get; set; } = [];
}

/// <summary>
/// Event arguments provided to the grid's OnRead callback.
/// The handler should populate <see cref="Data"/> and <see cref="Total"/>.
/// </summary>
public sealed class GridReadEventArgs
{
    /// <summary>
    /// The request details (page, page size, sorts).
    /// </summary>
    public GridRequest Request { get; set; } = new();

    /// <summary>
    /// Set this to the collection of items to display.
    /// </summary>
    public IEnumerable<object>? Data { get; set; }

    /// <summary>
    /// Set this to the total number of items (for paging).
    /// </summary>
    public int Total { get; set; }
}

/// <summary>
/// Event arguments provided when a grid row is clicked.
/// </summary>
public sealed class GridRowClickEventArgs
{
    /// <summary>
    /// The data item for the clicked row.
    /// </summary>
    public object? Item { get; set; }
}
