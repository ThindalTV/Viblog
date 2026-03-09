namespace Viblog.Infrastructure.Services;

/// <summary>
/// Provides text manipulation and formatting utilities
/// </summary>
public interface ITextUtilities
{
    /// <summary>
    /// Converts text to a URL-friendly slug
    /// </summary>
    /// <param name="text">The text to slugify</param>
    /// <returns>A URL-friendly slug, or null if the resulting slug would be empty</returns>
    string? Slugify(string? text);
}
