using EricJohansson.se.Models.Sitemap;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Repositories;
using Viblog.Infrastructure.Shared.Services;
using Viblog.Shared.Configuration;
using Viblog.Shared.Extensions;

namespace Viblog.Shared.Services;

/// <summary>
/// Service for generating sitemap data
/// </summary>
public class SitemapService : ISitemapService
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly IPageRepository _pageRepository;
    private readonly SiteMetadata _siteMetadata;

    public SitemapService(
        IBlogPostRepository blogPostRepository,
        IPageRepository pageRepository,
        IOptions<SiteMetadata> siteMetadata)
    {
        _blogPostRepository = blogPostRepository;
        _pageRepository = pageRepository;
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

        // Get all published pages (repository's GetBySlugAsync already handles scheduled promotion)
        var dukaPages = await _pageRepository.FindAsync(
            p => p.IsPublished,
            new PagingParameters { PageSize = 1000 },
            p => p.Slug,
            ascending: true,
            includeDeleted: false,
            cancellationToken);

        // Add custom pages to sitemap
        foreach (var page in dukaPages.Items)
        {
            var url = $"{_siteMetadata.BaseUrl}/{page.Slug}";
            var lastMod = page.UpdatedAt.UtcDateTime != default ? page.UpdatedAt.UtcDateTime : page.CreatedAt.UtcDateTime;
            
            urlSet.Urls.Add(CreateUrl(url, lastMod, "monthly", "0.8"));
        }

        // Get all published posts
        var posts = await _blogPostRepository.GetPublishedPostsAsync(
            new PagingParameters { PageSize = 10000 },
            cancellationToken);

        // Blog posts
        foreach (var post in posts.Items)
        {
            var url = $"{_siteMetadata.BaseUrl}/post/{post.PublishedAt!.Value.Year}/{post.Slug}";
            var lastMod = post.UpdatedAt.UtcDateTime != default ? post.UpdatedAt.UtcDateTime : post.PublishedAt!.Value.UtcDateTime;
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
            .Where(p => p.PublishedAt.HasValue)
            .Select(p => new { Year = p.PublishedAt!.Value.Year, Month = p.PublishedAt!.Value.Month })
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
