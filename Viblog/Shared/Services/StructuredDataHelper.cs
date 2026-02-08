using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Shared.Configuration;

namespace Viblog.Shared.Services;

/// <summary>
/// Helper service for generating JSON-LD structured data for SEO
/// </summary>
public class StructuredDataHelper
{
    private readonly SiteMetadata _siteMetadata;
    private readonly JsonSerializerOptions _jsonOptions;

    public StructuredDataHelper(IOptions<SiteMetadata> siteMetadata)
    {
        _siteMetadata = siteMetadata.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Generate WebSite schema for the homepage
    /// </summary>
    public string GenerateWebSiteSchema()
    {
        var schema = new
        {
            context = "https://schema.org",
            type = "WebSite",
            name = _siteMetadata.SiteName,
            url = _siteMetadata.BaseUrl,
            description = _siteMetadata.DefaultDescription,
            publisher = new
            {
                type = "Organization",
                name = _siteMetadata.SiteName,
                logo = !string.IsNullOrWhiteSpace(_siteMetadata.LogoUrl) ? new
                {
                    type = "ImageObject",
                    url = _siteMetadata.LogoUrl
                } : null
            },
            potentialAction = new
            {
                type = "SearchAction",
                target = new
                {
                    type = "EntryPoint",
                    urlTemplate = $"{_siteMetadata.BaseUrl}/search?q={{search_term_string}}"
                },
                queryInput = "required name=search_term_string"
            }
        };

        return JsonSerializer.Serialize(schema, _jsonOptions);
    }

    /// <summary>
    /// Generate Organization schema for the homepage
    /// </summary>
    public string GenerateOrganizationSchema()
    {
        var schema = new
        {
            context = "https://schema.org",
            type = "Organization",
            name = _siteMetadata.SiteName,
            url = _siteMetadata.BaseUrl,
            logo = !string.IsNullOrWhiteSpace(_siteMetadata.LogoUrl) ? new
            {
                type = "ImageObject",
                url = _siteMetadata.LogoUrl
            } : null,
            sameAs = new List<string>(),
            contactPoint = !string.IsNullOrWhiteSpace(_siteMetadata.ContactEmail) ? new
            {
                type = "ContactPoint",
                email = _siteMetadata.ContactEmail,
                contactType = "customer support"
            } : null
        };

        return JsonSerializer.Serialize(schema, _jsonOptions);
    }

    /// <summary>
    /// Generate BlogPosting schema for a blog post
    /// </summary>
    public string GenerateBlogPostingSchema(BlogPost post, string postUrl)
    {
        var imageObject = !string.IsNullOrWhiteSpace(post.FeaturedImageUrl) 
            ? new
            {
                type = "ImageObject",
                url = post.FeaturedImageUrl,
                caption = post.FeaturedImageAlt
            }
            : (!string.IsNullOrWhiteSpace(_siteMetadata.DefaultImageUrl) 
                ? new
                {
                    type = "ImageObject",
                    url = _siteMetadata.DefaultImageUrl,
                    caption = (string?)null
                } 
                : null);

        var schema = new
        {
            context = "https://schema.org",
            type = "BlogPosting",
            headline = post.Title,
            description = post.MetaDescription ?? post.Short,
            url = postUrl,
            datePublished = post.PublishedAt.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            dateModified = (post.UpdatedAt.UtcDateTime != default ? post.UpdatedAt.UtcDateTime : post.PublishedAt.UtcDateTime).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            author = new
            {
                type = "Person",
                name = post.AuthorName
            },
            publisher = new
            {
                type = "Organization",
                name = _siteMetadata.SiteName,
                logo = !string.IsNullOrWhiteSpace(_siteMetadata.LogoUrl) ? new
                {
                    type = "ImageObject",
                    url = _siteMetadata.LogoUrl
                } : null
            },
            image = imageObject,
            keywords = post.Tags != null && post.Tags.Any() ? string.Join(", ", post.Tags) : null,
            wordCount = CalculateWordCount(post.Content),
            articleBody = StripHtml(post.Content),
            mainEntityOfPage = new
            {
                type = "WebPage",
                id = postUrl
            }
        };

        return JsonSerializer.Serialize(schema, _jsonOptions);
    }

    /// <summary>
    /// Generate BreadcrumbList schema
    /// </summary>
    public string GenerateBreadcrumbSchema(List<(string name, string url)> breadcrumbs)
    {
        var itemListElements = breadcrumbs.Select((breadcrumb, index) => new
        {
            type = "ListItem",
            position = index + 1,
            name = breadcrumb.name,
            item = breadcrumb.url
        }).ToList();

        var schema = new
        {
            context = "https://schema.org",
            type = "BreadcrumbList",
            itemListElement = itemListElements
        };

        return JsonSerializer.Serialize(schema, _jsonOptions);
    }

    /// <summary>
    /// Generate CollectionPage schema for blog listing pages
    /// </summary>
    public string GenerateCollectionPageSchema(string pageUrl, string pageTitle, string? pageDescription = null)
    {
        var schema = new
        {
            context = "https://schema.org",
            type = "CollectionPage",
            name = pageTitle,
            description = pageDescription ?? _siteMetadata.DefaultDescription,
            url = pageUrl,
            isPartOf = new
            {
                type = "WebSite",
                name = _siteMetadata.SiteName,
                url = _siteMetadata.BaseUrl
            }
        };

        return JsonSerializer.Serialize(schema, _jsonOptions);
    }

    /// <summary>
    /// Calculate approximate word count from HTML content
    /// </summary>
    private int CalculateWordCount(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return 0;

        var plainText = StripHtml(htmlContent);
        var words = plainText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }

    /// <summary>
    /// Strip HTML tags from content for structured data
    /// </summary>
    private string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Simple HTML tag removal - for production, consider using HtmlAgilityPack
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }
}
