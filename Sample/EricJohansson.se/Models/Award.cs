namespace EricJohansson.se.Models;

/// <summary>
/// Represents an award or certification with image
/// </summary>
/// <param name="ImageUrl">The URL of the award image</param>
/// <param name="Title">The title of the award for hover tooltip</param>
/// <param name="AltText">Accessible description of the award</param>
/// <param name="Url">The URL to link to when the award is clicked</param>
public record Award(string ImageUrl, string Title, string AltText, string Url);
