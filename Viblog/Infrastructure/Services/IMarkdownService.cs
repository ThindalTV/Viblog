namespace Viblog.Infrastructure.Services;

/// <summary>
/// Provides markdown rendering services
/// </summary>
public interface IMarkdownService
{
    /// <summary>
    /// Converts markdown text to HTML
    /// </summary>
    /// <param name="markdown">The markdown content to render</param>
    /// <returns>The rendered HTML content, or empty string if markdown is null or whitespace</returns>
    string RenderToHtml(string? markdown);
}
