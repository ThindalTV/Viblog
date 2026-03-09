using Markdig;
using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Data.Entities.Content;

namespace Viblog.Shared.Services.Content;

/// <summary>
/// Content-related utilities: search indexing, reading time, markdown rendering.
/// </summary>
public class ContentProcessingService
{
    private readonly ILogger<ContentProcessingService> _logger;
    private readonly MarkdownPipeline _markdownPipeline;

    public ContentProcessingService(ILogger<ContentProcessingService> logger)
    {
        _logger = logger;
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    /// <summary>
    /// Updates search index for content.
    /// Includes title, markdown, and optional additional text (tags, categories, etc.)
    /// </summary>
    public virtual void UpdateSearchIndex(BaseContent content, string? additionalText = null)
    {
        var searchText = $"{content.Title} {content.Markdown} {additionalText ?? string.Empty}";
        
        // Simple search index: lowercase, remove extra whitespace
        content.SearchIndex = string.Join(" ", 
            searchText.ToLowerInvariant()
                      .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries));

        _logger.LogDebug("Updated search index for content (length: {Length})", content.SearchIndex.Length);
    }

    /// <summary>
    /// Calculates reading time in minutes based on average reading speed (200 words/min).
    /// </summary>
    public virtual int CalculateReadingTime(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return 0;
        }

        var wordCount = markdown.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var minutes = (int)Math.Ceiling(wordCount / 200.0);

        return Math.Max(1, minutes); // Minimum 1 minute
    }

    /// <summary>
    /// Renders markdown to HTML.
    /// </summary>
    public virtual string RenderMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        try
        {
            return Markdown.ToHtml(markdown, _markdownPipeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering markdown");
            return string.Empty;
        }
    }

    /// <summary>
    /// Computes content hash (delegates to ContentData).
    /// </summary>
    public virtual void ComputeContentHash(BaseContent content)
    {
        content.ComputeHash();
    }
}
