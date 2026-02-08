namespace Viblog.Frontend.Models;

/// <summary>
/// Represents a Twitter Card label/data pair for additional metadata
/// </summary>
public class TwitterMetadata
{
    /// <summary>
    /// Common Twitter Card label constants
    /// </summary>
    public static class Labels
    {
        /// <summary>
        /// Reading time label (e.g., "5 min read")
        /// </summary>
        public const string ReadingTime = "Reading time";

        /// <summary>
        /// Category label (e.g., "Technology", "Lifestyle")
        /// </summary>
        public const string Category = "Category";

        /// <summary>
        /// Author label (e.g., "John Doe")
        /// </summary>
        public const string Author = "Written by";

        /// <summary>
        /// Views/Read count label (e.g., "1,234 views")
        /// </summary>
        public const string Views = "Views";

        /// <summary>
        /// Published date label (e.g., "Jan 15, 2024")
        /// </summary>
        public const string PublishedDate = "Published";

        /// <summary>
        /// Updated date label (e.g., "Updated Jan 20, 2024")
        /// </summary>
        public const string UpdatedDate = "Updated";

        /// <summary>
        /// Word count label (e.g., "1,500 words")
        /// </summary>
        public const string WordCount = "Word count";

        /// <summary>
        /// Comment count label (e.g., "23 comments")
        /// </summary>
        public const string Comments = "Comments";

        /// <summary>
        /// Series/Collection label (e.g., "Part 3 of 5")
        /// </summary>
        public const string Series = "Series";

        /// <summary>
        /// Difficulty/Level label (e.g., "Beginner", "Advanced")
        /// </summary>
        public const string Level = "Level";

        /// <summary>
        /// Tags label (e.g., "C#, Blazor, SEO")
        /// </summary>
        public const string Tags = "Tags";

        /// <summary>
        /// Location label (e.g., "San Francisco, CA")
        /// </summary>
        public const string Location = "Location";
    }

    /// <summary>
    /// The label for the Twitter Card data (e.g., "Reading time", "Category")
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The data value for the Twitter Card (e.g., "5 min read", "Technology")
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new TwitterMetadata instance
    /// </summary>
    public TwitterMetadata()
    {
    }

    /// <summary>
    /// Creates a new TwitterMetadata instance with label and data
    /// </summary>
    /// <param name="label">The label</param>
    /// <param name="data">The data value</param>
    public TwitterMetadata(string label, string data)
    {
        Label = label;
        Data = data;
    }
}
