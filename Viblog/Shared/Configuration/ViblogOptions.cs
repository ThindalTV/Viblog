namespace Viblog.Shared.Configuration;

/// <summary>
/// Host-level options configured via <c>builder.AddViblog(options => { ... })</c>.
/// These are structural decisions made at composition time, not environment-specific settings.
/// </summary>
public class ViblogOptions
{
    private string? _previewStylesheetPath;

    /// <summary>
    /// Path to a CSS stylesheet applied to the content preview panel in the admin editor.
    /// Use the host site's public stylesheet so the preview approximates the published appearance.
    /// The value must be either a root-relative path such as <c>/css/blog.css</c> or an absolute URL.
    /// Relative paths such as <c>css/blog.css</c> are normalized to root-relative paths.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.AddViblog(options =>
    /// {
    ///     options.PreviewStylesheetPath = "/css/blog.css";
    ///     // or
    ///     options.PreviewStylesheetPath = "https://cdn.example.com/css/blog.css";
    /// });
    /// </code>
    /// </example>
    public string? PreviewStylesheetPath
    {
        get => _previewStylesheetPath;
        set => _previewStylesheetPath = NormalizePreviewStylesheetPath(value);
    }

    private static string? NormalizePreviewStylesheetPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out _)
            || value.StartsWith("/", StringComparison.Ordinal))
        {
            return value;
        }

        return "/" + value;
    }
}
