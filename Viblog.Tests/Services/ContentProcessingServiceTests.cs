using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Shared.Services.Content;

namespace Viblog.Tests.Services;

/// <summary>
/// Unit tests for ContentProcessingService.
/// </summary>
public class ContentProcessingServiceTests
{
    private readonly ContentProcessingService _service;

    public ContentProcessingServiceTests()
    {
        _service = new ContentProcessingService(Mock.Of<ILogger<ContentProcessingService>>());
    }

    #region UpdateSearchIndex

    [Fact]
    public void UpdateSearchIndex_IncludesTitleAndMarkdown()
    {
        var content = new BlogPostContent { Title = "Hello World", Markdown = "This is content" };

        _service.UpdateSearchIndex(content);

        Assert.Contains("hello", content.SearchIndex);
        Assert.Contains("world", content.SearchIndex);
        Assert.Contains("this", content.SearchIndex);
        Assert.Contains("content", content.SearchIndex);
    }

    [Fact]
    public void UpdateSearchIndex_IsNormalisedToLowerCase()
    {
        var content = new BlogPostContent { Title = "UPPER CASE", Markdown = "Mixed Case Text" };

        _service.UpdateSearchIndex(content);

        Assert.Equal(content.SearchIndex, content.SearchIndex.ToLowerInvariant());
    }

    [Fact]
    public void UpdateSearchIndex_IncludesAdditionalText()
    {
        var content = new BlogPostContent { Title = "Post", Markdown = "Content" };

        _service.UpdateSearchIndex(content, additionalText: "csharp blazor");

        Assert.Contains("csharp", content.SearchIndex);
        Assert.Contains("blazor", content.SearchIndex);
    }

    [Fact]
    public void UpdateSearchIndex_EmptyContent_ProducesEmptyIndex()
    {
        var content = new BlogPostContent { Title = "", Markdown = "" };

        _service.UpdateSearchIndex(content);

        Assert.Equal(string.Empty, content.SearchIndex);
    }

    #endregion

    #region CalculateReadingTime

    [Fact]
    public void CalculateReadingTime_TwoHundredWords_ReturnsOne()
    {
        var markdown = string.Join(" ", Enumerable.Repeat("word", 200));

        var result = _service.CalculateReadingTime(markdown);

        Assert.Equal(1, result);
    }

    [Fact]
    public void CalculateReadingTime_ZeroWords_ReturnsZero()
    {
        var result = _service.CalculateReadingTime(string.Empty);

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateReadingTime_FourHundredWords_ReturnsTwo()
    {
        var markdown = string.Join(" ", Enumerable.Repeat("word", 400));

        var result = _service.CalculateReadingTime(markdown);

        Assert.Equal(2, result);
    }

    [Fact]
    public void CalculateReadingTime_OneWord_ReturnsOne()
    {
        var result = _service.CalculateReadingTime("word");

        Assert.Equal(1, result); // Minimum 1 minute
    }

    [Fact]
    public void CalculateReadingTime_NullInput_ReturnsZero()
    {
        var result = _service.CalculateReadingTime(null!);

        Assert.Equal(0, result);
    }

    #endregion

    #region RenderMarkdown

    [Fact]
    public void RenderMarkdown_BasicMarkdown_ReturnsHtml()
    {
        var result = _service.RenderMarkdown("# Hello World");

        Assert.Contains("Hello World", result);
        Assert.Contains("<h1", result);
    }

    [Fact]
    public void RenderMarkdown_NullInput_ReturnsEmpty()
    {
        var result = _service.RenderMarkdown(null!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RenderMarkdown_EmptyInput_ReturnsEmpty()
    {
        var result = _service.RenderMarkdown(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    #endregion
}
