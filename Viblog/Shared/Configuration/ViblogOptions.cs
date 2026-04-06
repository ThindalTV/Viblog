namespace Viblog.Shared.Configuration;

/// <summary>
/// Host-level options configured via <c>builder.AddViblog(options => { ... })</c>.
/// These are structural decisions made at composition time, not environment-specific settings.
/// </summary>
public class ViblogOptions
{
    /// <summary>
    /// Path to a CSS stylesheet applied to the content preview panel in the admin editor.
    /// Use the host site's public stylesheet so the preview approximates the published appearance.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.AddViblog(options =>
    /// {
    ///     options.PreviewStylesheetPath = "/css/blog.css";
    /// });
    /// </code>
    /// </example>
    public string? PreviewStylesheetPath { get; set; }
}
