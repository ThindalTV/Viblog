using System.Text.Json;
using Microsoft.Extensions.Options;
using Viblog.Shared.Configuration;

namespace Viblog.Tests.Services;

public class StructuredDataHelperTests
{
    private readonly SiteMetadata _siteMetadata;
    private readonly StructuredDataHelper _helper;

    public StructuredDataHelperTests()
    {
        _siteMetadata = new SiteMetadata
        {
            BaseUrl = "https://example.com",
            SiteName = "Test Blog",
            DefaultDescription = "A test blog for testing",
            LogoUrl = "https://example.com/logo.png",
            ContactEmail = "contact@example.com",
            DefaultImageUrl = "https://example.com/default.jpg"
        };

        var options = Options.Create(_siteMetadata);
        _helper = new StructuredDataHelper(options);
    }

    #region GenerateWebSiteSchema Tests

    [Fact]
    public void GenerateWebSiteSchema_ReturnsValidJsonLd()
    {
        // Act
        var json = _helper.GenerateWebSiteSchema();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        Assert.Equal("https://schema.org", root.GetProperty("context").GetString());
        Assert.Equal("WebSite", root.GetProperty("type").GetString());
    }

    [Fact]
    public void GenerateWebSiteSchema_IncludesSiteMetadata()
    {
        // Act
        var json = _helper.GenerateWebSiteSchema();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.Equal(_siteMetadata.SiteName, root.GetProperty("name").GetString());
        Assert.Equal(_siteMetadata.BaseUrl, root.GetProperty("url").GetString());
        Assert.Equal(_siteMetadata.DefaultDescription, root.GetProperty("description").GetString());
    }

    [Fact]
    public void GenerateWebSiteSchema_IncludesPublisher()
    {
        // Act
        var json = _helper.GenerateWebSiteSchema();
        var doc = JsonDocument.Parse(json);
        var publisher = doc.RootElement.GetProperty("publisher");

        // Assert
        Assert.Equal("Organization", publisher.GetProperty("type").GetString());
        Assert.Equal(_siteMetadata.SiteName, publisher.GetProperty("name").GetString());
        Assert.Equal(_siteMetadata.LogoUrl, publisher.GetProperty("logo").GetProperty("url").GetString());
    }

    [Fact]
    public void GenerateWebSiteSchema_IncludesSearchAction()
    {
        // Act
        var json = _helper.GenerateWebSiteSchema();
        var doc = JsonDocument.Parse(json);
        var potentialAction = doc.RootElement.GetProperty("potentialAction");

        // Assert
        Assert.Equal("SearchAction", potentialAction.GetProperty("type").GetString());
        Assert.Equal("required name=search_term_string", potentialAction.GetProperty("queryInput").GetString());
        
        var target = potentialAction.GetProperty("target");
        Assert.Equal("EntryPoint", target.GetProperty("type").GetString());
        Assert.Contains("/search?q={search_term_string}", target.GetProperty("urlTemplate").GetString());
    }

    [Fact]
    public void GenerateWebSiteSchema_WithoutLogoUrl_OmitsLogo()
    {
        // Arrange
        var metadataWithoutLogo = new SiteMetadata
        {
            BaseUrl = "https://example.com",
            SiteName = "Test Blog",
            DefaultDescription = "Test",
            LogoUrl = null
        };
        var helper = new StructuredDataHelper(Options.Create(metadataWithoutLogo));

        // Act
        var json = helper.GenerateWebSiteSchema();
        var doc = JsonDocument.Parse(json);
        var publisher = doc.RootElement.GetProperty("publisher");

        // Assert
        Assert.False(publisher.TryGetProperty("logo", out _));
    }

    #endregion

    #region GenerateOrganizationSchema Tests

    [Fact]
    public void GenerateOrganizationSchema_ReturnsValidJsonLd()
    {
        // Act
        var json = _helper.GenerateOrganizationSchema();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        Assert.Equal("https://schema.org", root.GetProperty("context").GetString());
        Assert.Equal("Organization", root.GetProperty("type").GetString());
    }

    [Fact]
    public void GenerateOrganizationSchema_IncludesSiteMetadata()
    {
        // Act
        var json = _helper.GenerateOrganizationSchema();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.Equal(_siteMetadata.SiteName, root.GetProperty("name").GetString());
        Assert.Equal(_siteMetadata.BaseUrl, root.GetProperty("url").GetString());
    }

    [Fact]
    public void GenerateOrganizationSchema_IncludesLogo()
    {
        // Act
        var json = _helper.GenerateOrganizationSchema();
        var doc = JsonDocument.Parse(json);
        var logo = doc.RootElement.GetProperty("logo");

        // Assert
        Assert.Equal("ImageObject", logo.GetProperty("type").GetString());
        Assert.Equal(_siteMetadata.LogoUrl, logo.GetProperty("url").GetString());
    }

    [Fact]
    public void GenerateOrganizationSchema_IncludesContactPoint()
    {
        // Act
        var json = _helper.GenerateOrganizationSchema();
        var doc = JsonDocument.Parse(json);
        var contactPoint = doc.RootElement.GetProperty("contactPoint");

        // Assert
        Assert.Equal("ContactPoint", contactPoint.GetProperty("type").GetString());
        Assert.Equal(_siteMetadata.ContactEmail, contactPoint.GetProperty("email").GetString());
        Assert.Equal("customer support", contactPoint.GetProperty("contactType").GetString());
    }

    [Fact]
    public void GenerateOrganizationSchema_WithoutContactEmail_OmitsContactPoint()
    {
        // Arrange
        var metadataWithoutEmail = new SiteMetadata
        {
            BaseUrl = "https://example.com",
            SiteName = "Test Blog",
            ContactEmail = null
        };
        var helper = new StructuredDataHelper(Options.Create(metadataWithoutEmail));

        // Act
        var json = helper.GenerateOrganizationSchema();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.False(root.TryGetProperty("contactPoint", out _));
    }

    #endregion

    #region GenerateBlogPostingSchema Tests

    [Fact]
    public void GenerateBlogPostingSchema_ReturnsValidJsonLd()
    {
        // Arrange
        var post = CreateBlogPost();
        var postUrl = "https://example.com/post/2024/test-post";

        // Act
        var json = _helper.GenerateBlogPostingSchema(post, postUrl);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        Assert.Equal("https://schema.org", root.GetProperty("context").GetString());
        Assert.Equal("BlogPosting", root.GetProperty("type").GetString());
    }

    [Fact]
    public void GenerateBlogPostingSchema_IncludesPostMetadata()
    {
        // Arrange
        var post = CreateBlogPost();
        var postUrl = "https://example.com/post/2024/test-post";

        // Act
        var json = _helper.GenerateBlogPostingSchema(post, postUrl);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.Equal(post.Title, root.GetProperty("headline").GetString());
        Assert.Equal(post.Short, root.GetProperty("description").GetString());
        Assert.Equal(postUrl, root.GetProperty("url").GetString());
    }

    [Fact]
    public void GenerateBlogPostingSchema_UsesMetaDescriptionWhenAvailable()
    {
        // Arrange
        var post = CreateBlogPost();
        post.MetaDescription = "Custom meta description";
        var postUrl = "https://example.com/post/2024/test-post";

        // Act
        var json = _helper.GenerateBlogPostingSchema(post, postUrl);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.Equal("Custom meta description", root.GetProperty("description").GetString());
    }

    [Fact]
    public void GenerateBlogPostingSchema_IncludesAuthor()
    {
        // Arrange
        var post = CreateBlogPost();
        var postUrl = "https://example.com/post/2024/test-post";

        // Act
        var json = _helper.GenerateBlogPostingSchema(post, postUrl);
        var doc = JsonDocument.Parse(json);
        var author = doc.RootElement.GetProperty("author");

        // Assert
        Assert.Equal("Person", author.GetProperty("type").GetString());
        Assert.Equal(post.AuthorName, author.GetProperty("name").GetString());
    }

    [Fact]
    public void GenerateBlogPostingSchema_IncludesPublisher()
    {
        // Arrange
        var post = CreateBlogPost();
        var postUrl = "https://example.com/post/2024/test-post";

        // Act
        var json = _helper.GenerateBlogPostingSchema(post, postUrl);
        var doc = JsonDocument.Parse(json);
        var publisher = doc.RootElement.GetProperty("publisher");

        // Assert
        Assert.Equal("Organization", publisher.GetProperty("type").GetString());
        Assert.Equal(_siteMetadata.SiteName, publisher.GetProperty("name").GetString());
        Assert.Equal(_siteMetadata.LogoUrl, publisher.GetProperty("logo").GetProperty("url").GetString());
    }

    [Fact]
    public void GenerateBlogPostingSchema_IncludesDates()
    {
        // Arrange
        var publishedDate = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var updatedDate = new DateTimeOffset(2024, 2, 20, 14, 45, 0, TimeSpan.Zero);
        
        var post = CreateBlogPost();
        post.PublishedAt = publishedDate;
        post.UpdatedAt = updatedDate;
        
        var postUrl = "https://example.com/post/2024/test-post";

        // Act
        var json = _helper.GenerateBlogPostingSchema(post, postUrl);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.Equal("2024-01-15T10:30:00Z", root.GetProperty("datePublished").GetString());
        Assert.Equal("2024-02-20T14:45:00Z", root.GetProperty("dateModified").GetString());
    }

    [Fact]
    public void GenerateBlogPostingSchema_UsesPublishedDateWhenNoUpdatedDate()
    {
        // Arrange
        var publishedDate = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        
        var post = CreateBlogPost();
        post.PublishedAt = publishedDate;
        post.UpdatedAt = DateTimeOffset.MinValue;
        
        var postUrl = "https://example.com/post/2024/test-post";

        // Act
        var json = _helper.GenerateBlogPostingSchema(post, postUrl);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.Equal("2024-01-15T10:30:00Z", root.GetProperty("datePublished").GetString());
        Assert.Equal("2024-01-15T10:30:00Z", root.GetProperty("dateModified").GetString());
    }

    [Fact]
    public void GenerateBlogPostingSchema_IncludesFeaturedImage()
    {
        // Arrange
        var post = CreateBlogPost();
        post.FeaturedImageUrl = "https://example.com/featured.jpg";
        post.FeaturedImageAlt = "Featured image description";
        var postUrl = "https://example.com/post/2024/test-post";

        // Act
        var json = _helper.GenerateBlogPostingSchema(post, postUrl);
        var doc = JsonDocument.Parse(json);
        var image = doc.RootElement.GetProperty("image");

        // Assert
        Assert.Equal("ImageObject", image.GetProperty("type").GetString());
        Assert.Equal("https://example.com/featured.jpg", image.GetProperty("url").GetString());
        Assert.Equal("Featured image description", image.GetProperty("caption").GetString());
    }

    [Fact]
    public void GenerateBlogPostingSchema_UsesDefaultImageWhenNoFeaturedImage()
    {
        // Arrange
        var post = CreateBlogPost();
        post.FeaturedImageUrl = null;
        var postUrl = "https://example.com/post/2024/test-post";

        // Act
        var json = _helper.GenerateBlogPostingSchema(post, postUrl);
        var doc = JsonDocument.Parse(json);
        var image = doc.RootElement.GetProperty("image");

        // Assert
        Assert.Equal("ImageObject", image.GetProperty("type").GetString());
        Assert.Equal(_siteMetadata.DefaultImageUrl, image.GetProperty("url").GetString());
    }

    [Fact]
    public void GenerateBlogPostingSchema_IncludesTags()
    {
        // Arrange
        var post = CreateBlogPost();
        post.Tags = ["csharp", "blazor", "dotnet"];
        var postUrl = "https://example.com/post/2024/test-post";

        // Act
        var json = _helper.GenerateBlogPostingSchema(post, postUrl);
        var doc = JsonDocument.Parse(json);
        var keywords = doc.RootElement.GetProperty("keywords").GetString();

        // Assert
        Assert.Equal("csharp, blazor, dotnet", keywords);
    }

    [Fact]
    public void GenerateBlogPostingSchema_IncludesWordCount()
    {
        // Arrange
        var post = CreateBlogPost();
        post.Content = "<p>This is a test post with some content.</p>";
        var postUrl = "https://example.com/post/2024/test-post";

        // Act
        var json = _helper.GenerateBlogPostingSchema(post, postUrl);
        var doc = JsonDocument.Parse(json);
        var wordCount = doc.RootElement.GetProperty("wordCount").GetInt32();

        // Assert
        Assert.True(wordCount > 0);
    }

    [Fact]
    public void GenerateBlogPostingSchema_IncludesMainEntityOfPage()
    {
        // Arrange
        var post = CreateBlogPost();
        var postUrl = "https://example.com/post/2024/test-post";

        // Act
        var json = _helper.GenerateBlogPostingSchema(post, postUrl);
        var doc = JsonDocument.Parse(json);
        var mainEntity = doc.RootElement.GetProperty("mainEntityOfPage");

        // Assert
        Assert.Equal("WebPage", mainEntity.GetProperty("type").GetString());
        Assert.Equal(postUrl, mainEntity.GetProperty("id").GetString());
    }

    [Fact]
    public void GenerateBlogPostingSchema_StripsHtmlFromArticleBody()
    {
        // Arrange
        var post = CreateBlogPost();
        post.Content = "<h1>Title</h1><p>This is <strong>bold</strong> text.</p>";
        var postUrl = "https://example.com/post/2024/test-post";

        // Act
        var json = _helper.GenerateBlogPostingSchema(post, postUrl);
        var doc = JsonDocument.Parse(json);
        var articleBody = doc.RootElement.GetProperty("articleBody").GetString();

        // Assert
        Assert.DoesNotContain("<", articleBody);
        Assert.DoesNotContain(">", articleBody);
        Assert.Contains("Title", articleBody);
        Assert.Contains("This is bold text", articleBody);
    }

    #endregion

    #region GenerateBreadcrumbSchema Tests

    [Fact]
    public void GenerateBreadcrumbSchema_ReturnsValidJsonLd()
    {
        // Arrange
        var breadcrumbs = new List<(string name, string url)>
        {
            ("Home", "https://example.com"),
            ("Blog", "https://example.com/blog")
        };

        // Act
        var json = _helper.GenerateBreadcrumbSchema(breadcrumbs);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        Assert.Equal("https://schema.org", root.GetProperty("context").GetString());
        Assert.Equal("BreadcrumbList", root.GetProperty("type").GetString());
    }

    [Fact]
    public void GenerateBreadcrumbSchema_IncludesAllBreadcrumbs()
    {
        // Arrange
        var breadcrumbs = new List<(string name, string url)>
        {
            ("Home", "https://example.com"),
            ("Category", "https://example.com/category/tech"),
            ("Post", "https://example.com/post/2024/test")
        };

        // Act
        var json = _helper.GenerateBreadcrumbSchema(breadcrumbs);
        var doc = JsonDocument.Parse(json);
        var itemList = doc.RootElement.GetProperty("itemListElement");

        // Assert
        Assert.Equal(3, itemList.GetArrayLength());
    }

    [Fact]
    public void GenerateBreadcrumbSchema_IncludesCorrectPositions()
    {
        // Arrange
        var breadcrumbs = new List<(string name, string url)>
        {
            ("Home", "https://example.com"),
            ("Blog", "https://example.com/blog")
        };

        // Act
        var json = _helper.GenerateBreadcrumbSchema(breadcrumbs);
        var doc = JsonDocument.Parse(json);
        var itemList = doc.RootElement.GetProperty("itemListElement");

        // Assert
        Assert.Equal(1, itemList[0].GetProperty("position").GetInt32());
        Assert.Equal(2, itemList[1].GetProperty("position").GetInt32());
    }

    [Fact]
    public void GenerateBreadcrumbSchema_IncludesNamesAndUrls()
    {
        // Arrange
        var breadcrumbs = new List<(string name, string url)>
        {
            ("Home", "https://example.com"),
            ("Tech Category", "https://example.com/category/tech")
        };

        // Act
        var json = _helper.GenerateBreadcrumbSchema(breadcrumbs);
        var doc = JsonDocument.Parse(json);
        var itemList = doc.RootElement.GetProperty("itemListElement");

        // Assert
        Assert.Equal("Home", itemList[0].GetProperty("name").GetString());
        Assert.Equal("https://example.com", itemList[0].GetProperty("item").GetString());
        Assert.Equal("Tech Category", itemList[1].GetProperty("name").GetString());
        Assert.Equal("https://example.com/category/tech", itemList[1].GetProperty("item").GetString());
    }

    #endregion

    #region GenerateCollectionPageSchema Tests

    [Fact]
    public void GenerateCollectionPageSchema_ReturnsValidJsonLd()
    {
        // Arrange
        var pageUrl = "https://example.com/category/tech";
        var pageTitle = "Technology Posts";

        // Act
        var json = _helper.GenerateCollectionPageSchema(pageUrl, pageTitle);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        Assert.Equal("https://schema.org", root.GetProperty("context").GetString());
        Assert.Equal("CollectionPage", root.GetProperty("type").GetString());
    }

    [Fact]
    public void GenerateCollectionPageSchema_IncludesPageMetadata()
    {
        // Arrange
        var pageUrl = "https://example.com/category/tech";
        var pageTitle = "Technology Posts";
        var pageDescription = "All posts about technology";

        // Act
        var json = _helper.GenerateCollectionPageSchema(pageUrl, pageTitle, pageDescription);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.Equal(pageTitle, root.GetProperty("name").GetString());
        Assert.Equal(pageDescription, root.GetProperty("description").GetString());
        Assert.Equal(pageUrl, root.GetProperty("url").GetString());
    }

    [Fact]
    public void GenerateCollectionPageSchema_UsesDefaultDescriptionWhenNotProvided()
    {
        // Arrange
        var pageUrl = "https://example.com/category/tech";
        var pageTitle = "Technology Posts";

        // Act
        var json = _helper.GenerateCollectionPageSchema(pageUrl, pageTitle);
        var doc = JsonDocument.Parse(json);
        var description = doc.RootElement.GetProperty("description").GetString();

        // Assert
        Assert.Equal(_siteMetadata.DefaultDescription, description);
    }

    [Fact]
    public void GenerateCollectionPageSchema_IncludesIsPartOf()
    {
        // Arrange
        var pageUrl = "https://example.com/category/tech";
        var pageTitle = "Technology Posts";

        // Act
        var json = _helper.GenerateCollectionPageSchema(pageUrl, pageTitle);
        var doc = JsonDocument.Parse(json);
        var isPartOf = doc.RootElement.GetProperty("isPartOf");

        // Assert
        Assert.Equal("WebSite", isPartOf.GetProperty("type").GetString());
        Assert.Equal(_siteMetadata.SiteName, isPartOf.GetProperty("name").GetString());
        Assert.Equal(_siteMetadata.BaseUrl, isPartOf.GetProperty("url").GetString());
    }

    #endregion

    #region Helper Methods

    private static BlogPost CreateBlogPost()
    {
        return new BlogPost
        {
            Id = "1",
            Title = "Test Blog Post",
            Slug = "test-post",
            Content = "<p>This is test content.</p>",
            Short = "Test short description",
            IsPublished = true,
            PublishedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero),
            UpdatedAt = DateTimeOffset.MinValue,
            AuthorName = "Test Author",
            CategoryNames = ["Technology"],
            Tags = ["test"]
        };
    }

    #endregion
}
