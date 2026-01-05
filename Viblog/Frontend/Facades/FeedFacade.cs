using System.Xml;
using Microsoft.Extensions.Options;
using Viblog.Frontend.Infrastructure;
using Viblog.Shared.Configuration;
using Viblog.Shared.Data.Common;
using Viblog.Shared.Data.Entities;
using Viblog.Shared.Data.Repositories;
using Viblog.Shared.Models.Feeds;

namespace Viblog.Frontend.Facades;

/// <summary>
/// Facade implementation for RSS/Atom feed generation
/// </summary>
public class FeedFacade : IFeedFacade
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly SiteMetadata _siteMetadata;

    public FeedFacade(
        IBlogPostRepository blogPostRepository,
        IOptions<SiteMetadata> siteMetadata)
    {
        ArgumentNullException.ThrowIfNull(blogPostRepository);
        _blogPostRepository = blogPostRepository;
        _siteMetadata = siteMetadata.Value;
    }

    /// <inheritdoc/>
    public virtual async Task<RssFeed> GenerateRssFeedAsync(int maxPosts = 20, CancellationToken cancellationToken = default)
    {
        var posts = await GetRecentPostsAsync(maxPosts, cancellationToken);

        var feed = new RssFeed
        {
            Channel = new RssChannel
            {
                Title = _siteMetadata.SiteName,
                Link = _siteMetadata.BaseUrl,
                Description = _siteMetadata.DefaultDescription,
                Language = "en-us",
                LastBuildDate = DateTimeOffset.UtcNow.ToString("R"),
                AtomLink = new AtomLink
                {
                    Href = $"{_siteMetadata.BaseUrl}/feed.xml",
                    Rel = "self",
                    Type = "application/rss+xml"
                }
            }
        };

        foreach (var post in posts)
        {
            feed.Channel.Items.Add(CreateRssItem(post));
        }

        return feed;
    }

    /// <inheritdoc/>
    public virtual async Task<AtomFeed> GenerateAtomFeedAsync(int maxPosts = 20, CancellationToken cancellationToken = default)
    {
        var posts = await GetRecentPostsAsync(maxPosts, cancellationToken);

        var feed = new AtomFeed
        {
            Title = _siteMetadata.SiteName,
            Subtitle = _siteMetadata.DefaultDescription,
            Id = _siteMetadata.BaseUrl,
            Updated = posts.FirstOrDefault()?.PublishedAt.ToString("yyyy-MM-ddTHH:mm:ssZ") 
                ?? DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Links =
            [
                new AtomLink { Href = _siteMetadata.BaseUrl },
                new AtomLink { Href = $"{_siteMetadata.BaseUrl}/feed.xml", Rel = "self" }
            ]
        };

        foreach (var post in posts)
        {
            feed.Entries.Add(CreateAtomEntry(post));
        }

        return feed;
    }

    private async Task<IEnumerable<BlogPost>> GetRecentPostsAsync(int maxPosts, CancellationToken cancellationToken)
    {
        var pagingParams = new PagingParameters(1, maxPosts);
        var result = await _blogPostRepository.GetPublishedPostsAsync(pagingParams, cancellationToken);
        return result.Items;
    }

    private RssItem CreateRssItem(BlogPost post)
    {
        var item = new RssItem
        {
            Title = post.Title,
            Link = $"{_siteMetadata.BaseUrl}/post/{post.Slug}",
            Guid = $"{_siteMetadata.BaseUrl}/post/{post.Slug}",
            Description = post.Short,
            Author = post.AuthorName,
            Categories = [.. post.CategoryNames]
        };

        item.PubDate = post.PublishedAt.ToString("R");

        if (!string.IsNullOrWhiteSpace(post.Content))
        {
            var doc = new XmlDocument();
            item.Content = doc.CreateCDataSection(post.Content);
        }

        return item;
    }

    private AtomEntry CreateAtomEntry(BlogPost post)
    {
        var entry = new AtomEntry
        {
            Title = post.Title,
            Id = $"{_siteMetadata.BaseUrl}/post/{post.Slug}",
            Summary = post.Short,
            Links = [new AtomLink { Href = $"{_siteMetadata.BaseUrl}/post/{post.Slug}" }],
            Author = new AtomPerson { Name = post.AuthorName }
        };

        entry.Published = post.PublishedAt.ToString("yyyy-MM-ddTHH:mm:ssZ");
        entry.Updated = post.PublishedAt.ToString("yyyy-MM-ddTHH:mm:ssZ");

        if (!string.IsNullOrWhiteSpace(post.Content))
        {
            var doc = new XmlDocument();
            var cdata = doc.CreateCDataSection(post.Content);
            entry.Content = new AtomContent
            {
                Type = "html",
                Value = [cdata]
            };
        }

        foreach (var category in post.CategoryNames)
        {
            entry.Categories.Add(new AtomCategory { Term = category });
        }

        return entry;
    }
}
