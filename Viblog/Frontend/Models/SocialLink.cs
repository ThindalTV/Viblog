namespace Viblog.Frontend.Models;

/// <summary>
/// Represents a social media link with URL, label, and icon
/// </summary>
/// <param name="Url">The URL of the social media profile</param>
/// <param name="Label">The accessible label for the link</param>
/// <param name="Icon">SVG icon markup</param>
public record SocialLink(string Url, string Label, string Icon);
