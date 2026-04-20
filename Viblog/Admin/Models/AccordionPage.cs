namespace Viblog.Admin.Models;

/// <summary>
/// Represents a single section/page in an accordion component.
/// </summary>
/// <typeparam name="T">The type of data contained in this accordion section</typeparam>
public class AccordionPage<T>
{
    /// <summary>
    /// Unique identifier for this accordion section, used for expansion tracking
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The data payload for this accordion section
    /// </summary>
    public T Data { get; set; } = default!;
}
