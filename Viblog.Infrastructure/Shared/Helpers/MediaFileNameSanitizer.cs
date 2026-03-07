using System.Text.RegularExpressions;

namespace Viblog.Infrastructure.Shared.Helpers;

/// <summary>
/// Sanitizes filenames so they are safe for blob storage and can be used directly
/// in URL paths without percent-encoding.
/// </summary>
public static class MediaFileNameSanitizer
{
    // Matches Windows-style copy suffixes: " (1)", "(2)", " (10) " etc.
    private static readonly Regex _copyNumberSuffix = new(@"\s*\(\d+\)\s*", RegexOptions.Compiled);

    /// <summary>
    /// Sanitizes a base filename (no extension) applying rules in order:
    /// <list type="number">
    ///   <item>Strip Windows copy suffixes such as <c>" (1)"</c>, <c>" (2)"</c></item>
    ///   <item>Replace spaces with underscores</item>
    ///   <item>Remove every character that is not ASCII alphanumeric, hyphen, underscore, or dot</item>
    ///   <item>Trim leading and trailing underscores and hyphens</item>
    /// </list>
    /// Returns <c>"file"</c> if the result would be empty.
    /// </summary>
    public static string Sanitize(string fileName)
    {
        // 1. Remove Windows-style copy suffixes: " (1)", "(2)", " (10)" etc.
        var sanitized = _copyNumberSuffix.Replace(fileName, string.Empty);

        // 2. Replace spaces with underscores
        sanitized = sanitized.Replace(' ', '_');

        // 3. Keep only characters that are safe in a URL path without encoding
        sanitized = string.Concat(sanitized.Where(
            c => char.IsAsciiLetterOrDigit(c) || c == '_' || c == '-' || c == '.'));

        // 4. Trim underscores and hyphens left at the edges after removals
        sanitized = sanitized.Trim('_').Trim('-');

        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }

    /// <summary>
    /// Returns the first available path in the sequence:
    /// <c>{prefix}{baseName}{extension}</c>,
    /// <c>{prefix}{baseName}_1{extension}</c>,
    /// <c>{prefix}{baseName}_2{extension}</c>, …
    /// up to 999 attempts, then falls back to a GUID suffix.
    /// </summary>
    /// <param name="prefix">Path prefix including trailing slash, e.g. <c>"images/2025/03/"</c></param>
    /// <param name="baseName">Sanitized base filename without extension</param>
    /// <param name="extension">File extension including leading dot, e.g. <c>".jpg"</c></param>
    /// <param name="pathExistsAsync">Returns <c>true</c> if the given full path is already taken</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task<string> ResolveCollisionAsync(
        string prefix,
        string baseName,
        string extension,
        Func<string, CancellationToken, Task<bool>> pathExistsAsync,
        CancellationToken cancellationToken = default)
    {
        var path = $"{prefix}{baseName}{extension}";
        if (!await pathExistsAsync(path, cancellationToken))
            return path;

        for (var i = 1; i < 1000; i++)
        {
            path = $"{prefix}{baseName}_{i}{extension}";
            if (!await pathExistsAsync(path, cancellationToken))
                return path;
        }

        // All numbered slots exhausted — use a GUID to guarantee uniqueness
        return $"{prefix}{baseName}_{Guid.NewGuid():N}{extension}";
    }
}
