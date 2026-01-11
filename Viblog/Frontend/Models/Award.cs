namespace Viblog.Frontend.Models;

/// <summary>
/// Represents an award or certification with image
/// </summary>
/// <param name="ImageUrl">The URL of the award image</param>
/// <param name="Title">The title of the award for hover tooltip</param>
/// <param name="AltText">Accessible description of the award</param>
public record Award(string ImageUrl, string Title, string AltText);
