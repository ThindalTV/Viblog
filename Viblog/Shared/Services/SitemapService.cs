using Microsoft.Extensions.Options;
using Viblog.Shared.Configuration;
using Viblog.Shared.Data.Repositories;
using Viblog.Shared.Models.Sitemap;

namespace Viblog.Shared.Services;

/// <summary>
/// Service for generating sitemap data
/// </summary>
public class SitemapService : ISitemapService
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly SiteMetadata _siteMetadata;

    public SitemapService(
        IBlogPostRepository blogPostRepository,
        IOptions<SiteMetadata> siteMetadata)
    {
        _blogPostRepository = blogPostRepository;
        _siteMetadata = siteMetadata.Value;
    }

    /// <summary>
    /// Generate sitemap data containing all public pages
    /// </summary>
    public async Task<SitemapUrlSet> GenerateSitemapAsync(CancellationToken cancellationToken = default)
    {
        var urlSet = new SitemapUrlSet();

        // Homepage
        urlSet.Urls.Add(CreateUrl(_siteMetadata.BaseUrl, DateTime.UtcNow, "daily", "1.0"));

        // Static pages
        urlSet.Urls.Add(CreateUrl($"{_siteMetadata.BaseUrl}/posts", DateTime.UtcNow, "daily", "0.9"));
        urlSet.Urls.Add(CreateUrl($"{_siteMetadata.BaseUrl}/archive", DateTime.UtcNow, "weekly", "0.8"));

        // Get all published posts
        var posts = await _blogPostRepository.GetPublishedPostsAsync(
            new Shared.Data.Common.PagingParameters { PageSize = 10000 },
            cancellationToken);

        // Blog posts
        foreach (var post in posts.Items)
        {
            var url = $"{_siteMetadata.BaseUrl}/post/{post.PublishedAt.Year}/{post.Slug}";
            var lastMod = post.UpdatedAt.UtcDateTime != default ? post.UpdatedAt.UtcDateTime : post.PublishedAt.UtcDateTime;
            var priority = post.IsFeatured ? "0.9" : "0.7";

            urlSet.Urls.Add(CreateUrl(url, lastMod, "monthly", priority));
        }

        // Get unique categories
        var categories = posts.Items
            .SelectMany(p => p.CategoryNames)
            .Distinct()
            .OrderBy(c => c);

        foreach (var category in categories)
        {
            var url = $"{_siteMetadata.BaseUrl}/category/{Uri.EscapeDataString(category)}";
            urlSet.Urls.Add(CreateUrl(url, DateTime.UtcNow, "weekly", "0.6"));
        }

        // Get unique tags
        var tags = posts.Items
            .Where(p => p.Tags != null)
            .SelectMany(p => p.Tags)
            .Distinct()
            .OrderBy(t => t);

        foreach (var tag in tags)
        {
            var url = $"{_siteMetadata.BaseUrl}/tag/{Uri.EscapeDataString(tag)}";
            urlSet.Urls.Add(CreateUrl(url, DateTime.UtcNow, "weekly", "0.6"));
        }

        // Get archive dates (year/month combinations)
        var archiveDates = posts.Items
            .Select(p => new { Year = p.PublishedAt.Year, Month = p.PublishedAt.Month })
            .Distinct()
            .OrderByDescending(d => d.Year)
            .ThenByDescending(d => d.Month);

        foreach (var date in archiveDates)
        {
            var url = $"{_siteMetadata.BaseUrl}/archive/{date.Year}/{date.Month:D2}";
            urlSet.Urls.Add(CreateUrl(url, DateTime.UtcNow, "monthly", "0.5"));
        }

        return urlSet;
    }

    /// <summary>
    /// Create a sitemap URL entry
    /// </summary>
    private static SitemapUrl CreateUrl(
        string location,
        DateTime lastModified,
        string changeFrequency,
        string priority)
    {
        return new SitemapUrl
        {
            Location = location,
            LastModified = lastModified.ToString("yyyy-MM-dd"),
            ChangeFrequency = changeFrequency,
            Priority = priority
        };
    }
}
