using Viblog.Infrastructure.Shared.Helpers;

namespace Viblog.Tests.Helpers;

/// <summary>
/// Unit tests for <see cref="MediaFileNameSanitizer"/>
/// </summary>
public class MediaFileNameSanitizerTests
{
    #region Windows copy-suffix stripping

    [Theory]
    [InlineData("photo (1)",   "photo")]
    [InlineData("photo (2)",   "photo")]
    [InlineData("photo (10)",  "photo")]
    [InlineData("photo (123)", "photo")]
    public void Sanitize_WithWindowsCopySuffix_RemovesSuffix(string input, string expected)
    {
        Assert.Equal(expected, MediaFileNameSanitizer.Sanitize(input));
    }

    [Theory]
    [InlineData("photo (1) ",  "photo")]  // trailing space after suffix
    [InlineData("photo  (1)",  "photo")]  // extra space before suffix
    [InlineData(" (1) photo",  "photo")]  // suffix at start
    public void Sanitize_WithWindowsCopySuffixAndExtraSpaces_RemovesSuffixAndTrimsBoundaries(string input, string expected)
    {
        Assert.Equal(expected, MediaFileNameSanitizer.Sanitize(input));
    }

    [Fact]
    public void Sanitize_WithNoSuffix_LeavesNameUnchanged()
    {
        Assert.Equal("photo", MediaFileNameSanitizer.Sanitize("photo"));
    }

    [Fact]
    public void Sanitize_WithParenthesesButNoDigits_RemovesParenthesesAsUnsafe()
    {
        // "(abc)" is NOT a copy suffix — the inner chars are stripped by the whitelist step
        Assert.Equal("abc", MediaFileNameSanitizer.Sanitize("(abc)"));
    }

    #endregion

    #region Space handling

    [Fact]
    public void Sanitize_WithInternalSpaces_ReplacesWithUnderscores()
    {
        Assert.Equal("my_photo", MediaFileNameSanitizer.Sanitize("my photo"));
    }

    [Fact]
    public void Sanitize_WithLeadingAndTrailingSpaces_TrimsResult()
    {
        Assert.Equal("photo", MediaFileNameSanitizer.Sanitize("  photo  "));
    }

    [Fact]
    public void Sanitize_WithMultipleConsecutiveSpaces_PreservesMultipleUnderscores()
    {
        Assert.Equal("my___photo", MediaFileNameSanitizer.Sanitize("my   photo"));
    }

    #endregion

    #region URL-unsafe character removal

    [Theory]
    [InlineData("my#photo",      "myphoto")]
    [InlineData("price50%off",   "price50off")]
    [InlineData("a+b",           "ab")]
    [InlineData("image@2024",    "image2024")]
    [InlineData("what!",         "what")]
    [InlineData("[bracket]",     "bracket")]
    [InlineData("{curly}",       "curly")]
    [InlineData("a^b",           "ab")]
    public void Sanitize_WithUrlUnsafeCharacters_RemovesThem(string input, string expected)
    {
        Assert.Equal(expected, MediaFileNameSanitizer.Sanitize(input));
    }

    #endregion

    #region Preserved safe characters

    [Fact]
    public void Sanitize_WithHyphens_PreservesHyphens()
    {
        Assert.Equal("my-photo", MediaFileNameSanitizer.Sanitize("my-photo"));
    }

    [Fact]
    public void Sanitize_WithDots_PreservesDots()
    {
        Assert.Equal("version.2.0", MediaFileNameSanitizer.Sanitize("version.2.0"));
    }

    [Fact]
    public void Sanitize_WithUnderscores_PreservesUnderscores()
    {
        Assert.Equal("my_photo", MediaFileNameSanitizer.Sanitize("my_photo"));
    }

    [Fact]
    public void Sanitize_WithDigits_PreservesDigits()
    {
        Assert.Equal("photo2024", MediaFileNameSanitizer.Sanitize("photo2024"));
    }

    #endregion

    #region Edge trimming

    [Theory]
    [InlineData("_leading",          "leading")]
    [InlineData("trailing_",         "trailing")]
    [InlineData("_both_",            "both")]
    [InlineData("-leading",          "leading")]
    [InlineData("trailing-",         "trailing")]
    [InlineData("-both-",            "both")]
    public void Sanitize_WithLeadingOrTrailingUnderscoreOrHyphen_Trims(string input, string expected)
    {
        Assert.Equal(expected, MediaFileNameSanitizer.Sanitize(input));
    }

    #endregion

    #region Fallback to "file"

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#!@")]
    [InlineData("(1)")]   // pure copy suffix — nothing left after removal
    public void Sanitize_WhenResultIsEmpty_ReturnsFallback(string input)
    {
        Assert.Equal("file", MediaFileNameSanitizer.Sanitize(input));
    }

    #endregion

    #region Combined scenarios

    [Fact]
    public void Sanitize_WithSpacesAndCopySuffix_RemovesSuffixAndReplacesSpaces()
    {
        Assert.Equal("My_Photo", MediaFileNameSanitizer.Sanitize("My Photo (2)"));
    }

    [Fact]
    public void Sanitize_WithUnsafeCharsAndSpaces_RemovesUnsafeAndReplacesSpaces()
    {
        // "Summer Vacation - Final!" → spaces→_, ! removed
        Assert.Equal("Summer_Vacation_-_Final", MediaFileNameSanitizer.Sanitize("Summer Vacation - Final!"));
    }

    [Fact]
    public void Sanitize_FullWindowsStyleDuplicate_ProducesCleanName()
    {
        // "Report (3) - Draft!"
        // step 1: regex \s*\(\d+\)\s* consumes " (3) " → "Report- Draft!"
        // step 2: space → _ → "Report-_Draft!"
        // step 3: whitelist removes ! → "Report-_Draft"
        Assert.Equal("Report-_Draft", MediaFileNameSanitizer.Sanitize("Report (3) - Draft!"));
    }

    /// <summary>
    /// Confirms that our collision suffixes (_1, _2 …) are appended to the already-sanitized
    /// base name, so the final stored name is always clean.
    /// </summary>
    [Theory]
    [InlineData("My Photo (1)", "_1", "My_Photo_1")]
    [InlineData("My Photo (1)", "_2", "My_Photo_2")]
    public void Sanitize_ThenAppendCollisionSuffix_ProducesCleanFinalName(
        string rawFileName, string collisionSuffix, string expectedFinal)
    {
        var sanitized = MediaFileNameSanitizer.Sanitize(rawFileName);
        var final = sanitized + collisionSuffix;
        Assert.Equal(expectedFinal, final);
    }

    #endregion

    #region Collision resolution

    [Fact]
    public async Task ResolveCollisionAsync_WhenNoConflict_ReturnsBaseName()
    {
        var result = await MediaFileNameSanitizer.ResolveCollisionAsync(
            prefix: "images/2025/03/",
            baseName: "photo",
            extension: ".jpg",
            pathExistsAsync: (_, _) => Task.FromResult(false));

        Assert.Equal("images/2025/03/photo.jpg", result);
    }

    [Fact]
    public async Task ResolveCollisionAsync_WhenBaseNameTaken_ReturnsFirstSuffix()
    {
        var taken = new HashSet<string> { "images/2025/03/photo.jpg" };

        var result = await MediaFileNameSanitizer.ResolveCollisionAsync(
            prefix: "images/2025/03/",
            baseName: "photo",
            extension: ".jpg",
            pathExistsAsync: (path, _) => Task.FromResult(taken.Contains(path)));

        Assert.Equal("images/2025/03/photo_1.jpg", result);
    }

    [Fact]
    public async Task ResolveCollisionAsync_WhenSeveralSuffixesTaken_ReturnsNextAvailable()
    {
        var taken = new HashSet<string>
        {
            "images/2025/03/photo.jpg",
            "images/2025/03/photo_1.jpg",
            "images/2025/03/photo_2.jpg",
        };

        var result = await MediaFileNameSanitizer.ResolveCollisionAsync(
            prefix: "images/2025/03/",
            baseName: "photo",
            extension: ".jpg",
            pathExistsAsync: (path, _) => Task.FromResult(taken.Contains(path)));

        Assert.Equal("images/2025/03/photo_3.jpg", result);
    }

    [Fact]
    public async Task ResolveCollisionAsync_WhenAllNumberedSlotsTaken_ReturnGuidSuffix()
    {
        // Build a set containing the base name and all _1 … _999 variants
        var taken = new HashSet<string> { "images/2025/03/photo.jpg" };
        for (var i = 1; i < 1000; i++)
            taken.Add($"images/2025/03/photo_{i}.jpg");

        var result = await MediaFileNameSanitizer.ResolveCollisionAsync(
            prefix: "images/2025/03/",
            baseName: "photo",
            extension: ".jpg",
            pathExistsAsync: (path, _) => Task.FromResult(taken.Contains(path)));

        // Result must not be any of the known paths and must match the GUID-suffix pattern
        Assert.DoesNotContain(result, taken);
        Assert.StartsWith("images/2025/03/photo_", result);
        Assert.EndsWith(".jpg", result);
    }

    [Fact]
    public async Task ResolveCollisionAsync_AfterSanitize_SuffixIsAppendedToCleanName()
    {
        // A raw filename with a Windows copy suffix — sanitize first, then resolve collisions
        var baseName = MediaFileNameSanitizer.Sanitize("My Photo (1)");
        var taken = new HashSet<string> { $"images/2025/03/{baseName}.jpg" };

        var result = await MediaFileNameSanitizer.ResolveCollisionAsync(
            prefix: "images/2025/03/",
            baseName: baseName,
            extension: ".jpg",
            pathExistsAsync: (path, _) => Task.FromResult(taken.Contains(path)));

        Assert.Equal("images/2025/03/My_Photo_1.jpg", result);
    }

    #endregion
}
