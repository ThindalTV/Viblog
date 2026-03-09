using Viblog.Infrastructure.Services;

namespace Viblog.Tests.Shared.Services;

/// <summary>
/// Tests for TextUtilities service
/// </summary>
public class TextUtilitiesTests
{
    private readonly ITextUtilities _textUtilities;

    public TextUtilitiesTests()
    {
        _textUtilities = new TextUtilities();
    }

    [Fact]
    public void Slugify_WithSimpleText_ReturnsLowercaseHyphenatedSlug()
    {
        // Arrange
        var text = "Hello World";

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void Slugify_WithMultipleSpaces_ReplacesWithSingleHyphen()
    {
        // Arrange
        var text = "Hello    World    Test";

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Equal("hello-world-test", result);
    }

    [Fact]
    public void Slugify_WithSpecialCharacters_RemovesInvalidCharacters()
    {
        // Arrange
        var text = "Hello @World! #Test$";

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Equal("hello-world-test", result);
    }

    [Fact]
    public void Slugify_WithNumbers_PreservesNumbers()
    {
        // Arrange
        var text = "Top 10 Posts of 2024";

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Equal("top-10-posts-of-2024", result);
    }

    [Fact]
    public void Slugify_WithLeadingAndTrailingSpaces_TrimsHyphens()
    {
        // Arrange
        var text = "  Hello World  ";

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void Slugify_WithConsecutiveHyphens_RemovesExtraHyphens()
    {
        // Arrange
        var text = "Hello---World";

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void Slugify_WithMixedCase_ConvertsToLowercase()
    {
        // Arrange
        var text = "HeLLo WoRLd";

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void Slugify_WithEmptyString_ReturnsNull()
    {
        // Arrange
        var text = "";

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Slugify_WithNull_ReturnsNull()
    {
        // Arrange
        string? text = null;

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Slugify_WithWhitespaceOnly_ReturnsNull()
    {
        // Arrange
        var text = "   ";

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Slugify_WithUnicodeCharacters_ConvertsToAsciiEquivalents()
    {
        // Arrange
        var text = "Héllo Wörld Tëst";

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Equal("hello-world-test", result);
    }

    [Fact]
    public void Slugify_WithUnderscores_RemovesUnderscores()
    {
        // Arrange
        var text = "Hello_World_Test";

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Equal("helloworldtest", result);
    }

    [Fact]
    public void Slugify_WithDashes_PreservesDashes()
    {
        // Arrange
        var text = "Hello-World";

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Equal("hello-world", result);
    }

    [Theory]
    [InlineData("C# Programming", "c-programming")]
    [InlineData("ASP.NET Core", "aspnet-core")]
    [InlineData("10 Tips & Tricks", "10-tips-tricks")]
    [InlineData("My First Post!", "my-first-post")]
    [InlineData("Questions???", "questions")]
    public void Slugify_WithRealWorldExamples_ReturnsExpectedSlugs(string input, string expected)
    {
        // Act
        var result = _textUtilities.Slugify(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Café", "cafe")]
    [InlineData("naïve", "naive")]
    [InlineData("Zürich", "zurich")]
    [InlineData("São Paulo", "sao-paulo")]
    [InlineData("Montréal", "montreal")]
    [InlineData("François", "francois")]
    [InlineData("Ångström", "angstrom")]
    [InlineData("\u0141\u00F3d\u017A", "lodz")]  // ?ód? using Unicode escapes
    [InlineData("Kraków", "krakow")]
    [InlineData("Malmö", "malmo")]
    public void Slugify_WithInternationalCharacters_ConvertsToAscii(string input, string expected)
    {
        // Act
        var result = _textUtilities.Slugify(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello ??", "hello")]  // Japanese/Chinese characters
    [InlineData("????? World", "world")]  // Japanese Hiragana
    [InlineData("???? Test", "test")]  // Japanese Katakana
    [InlineData("????? Hello", "hello")]  // Arabic
    [InlineData("?????? World", "world")]  // Cyrillic (Russian)
    [InlineData("????? Greeting", "greeting")]  // Korean
    [InlineData("Test ?? Emoji", "test-emoji")]  // Emoji
    [InlineData("Mixed ?? English ???", "mixed-english")]  // Multiple scripts
    [InlineData("?? Celebration ??", "celebration")]  // Multiple emoji
    public void Slugify_WithUnmappableCharacters_RemovesThem(string input, string expected)
    {
        // Act
        var result = _textUtilities.Slugify(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Slugify_WithOnlyUnmappableCharacters_ReturnsNull()
    {
        // Arrange - Only non-ASCII characters that can't be mapped
        var text = "??";

        // Act
        var result = _textUtilities.Slugify(text);

        // Assert
        Assert.Null(result);
    }
}
