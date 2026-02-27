using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Infrastructure.Shared.Extensions;
using Viblog.Shared.Extensions;

namespace Viblog.Shared.Services.Content;

/// <summary>
/// Manages version history, snapshots, and Draft/Live promotion.
/// Version history persisted to separate repository for scalability.
/// </summary>
public class ContentVersionService
{
    private readonly IBlogPostVersionRepository _blogPostVersionRepository;
    private readonly IPageVersionRepository _pageVersionRepository;
    private readonly ILogger<ContentVersionService> _logger;

    public ContentVersionService(
        IBlogPostVersionRepository blogPostVersionRepository,
        IPageVersionRepository pageVersionRepository,
        ILogger<ContentVersionService> logger)
    {
        _blogPostVersionRepository = blogPostVersionRepository;
        _pageVersionRepository = pageVersionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Promotes Draft to Live and creates version snapshot.
    /// This is the core publishing operation.
    /// </summary>
    public virtual async Task PromoteDraftToLiveAsync(ISchedulableContent content, string publishedBy, string? publishedByName = null, string? changeNote = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Promoting Draft to Live for content {ContentId}", content.Id);

        // Clone Draft to Live
        var draftContent = content.GetDraftContent();

        if (draftContent == null)
        {
            throw new InvalidOperationException("Cannot publish content without Draft");
        }

        var clonedContent = CloneContent(draftContent);
        content.SetLiveContent(clonedContent);

        // Create version snapshot (saves to repository)
        await CreatePublishedSnapshotAsync(content, publishedBy, publishedByName, changeNote, cancellationToken);

        // Apply retention policy
        await ApplyRetentionPolicyAsync(content, cancellationToken);

        _logger.LogInformation("Draft promoted to Live for content {ContentId}", content.Id);
    }

    /// <summary>
    /// Creates immutable snapshot of Live content and saves to repository.
    /// Uses pattern matching to create type-specific version entities.
    /// </summary>
    public virtual async Task CreatePublishedSnapshotAsync(ISchedulableContent content, string publishedBy, string? publishedByName = null, string? changeNote = null, CancellationToken cancellationToken = default)
    {
        if (content is BlogPost blogPost)
        {
            if (blogPost.Live == null)
            {
                _logger.LogWarning("Cannot create snapshot for BlogPost {ContentId} - no Live version", content.Id);
                return;
            }

            var versionNumber = await _blogPostVersionRepository.GetLatestVersionNumberAsync(blogPost.Id, cancellationToken) + 1;

            var snapshot = new BlogPostVersion
            {
                ContentId = blogPost.Id,
                Content = CloneContent(blogPost.Live) as BlogPostContent ?? throw new InvalidOperationException("Failed to clone BlogPostContent"),
                PublishedAt = DateTimeOffset.UtcNow,
                PublishedBy = publishedBy,
                PublishedByName = publishedByName ?? publishedBy,
                ChangeNote = changeNote,
                VersionNumber = versionNumber
            };

            await _blogPostVersionRepository.AddAsync(snapshot, cancellationToken);
            await _blogPostVersionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created version {Version} for BlogPost {ContentId}", versionNumber, content.Id);
        }
        else if (content is Page page)
        {
            if (page.Live == null)
            {
                _logger.LogWarning("Cannot create snapshot for Page {ContentId} - no Live version", content.Id);
                return;
            }

            var versionNumber = await _pageVersionRepository.GetLatestVersionNumberAsync(page.Id, cancellationToken) + 1;

            var snapshot = new PageVersion
            {
                ContentId = page.Id,
                Content = CloneContent(page.Live) as PageContent ?? throw new InvalidOperationException("Failed to clone PageContent"),
                PublishedAt = DateTimeOffset.UtcNow,
                PublishedBy = publishedBy,
                PublishedByName = publishedByName ?? publishedBy,
                ChangeNote = changeNote,
                VersionNumber = versionNumber
            };

            await _pageVersionRepository.AddAsync(snapshot, cancellationToken);
            await _pageVersionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created version {Version} for Page {ContentId}", versionNumber, content.Id);
        }
    }

    /// <summary>
    /// Applies retention policy: last 10 + monthly for 2 years + yearly forever.
    /// Queries versions from repository and deletes old ones.
    /// </summary>
    public virtual async Task ApplyRetentionPolicyAsync(ISchedulableContent content, CancellationToken cancellationToken = default)
    {
        if (content is BlogPost blogPost)
        {
            var versions = (await _blogPostVersionRepository.GetVersionsForContentAsync(blogPost.Id, cancellationToken)).ToList();
            await ApplyRetentionPolicyToVersionsAsync(versions, blogPost.Id, _blogPostVersionRepository, cancellationToken);
        }
        else if (content is Page page)
        {
            var versions = (await _pageVersionRepository.GetVersionsForContentAsync(page.Id, cancellationToken)).ToList();
            await ApplyRetentionPolicyToVersionsAsync(versions, page.Id, _pageVersionRepository, cancellationToken);
        }
    }

    private async Task ApplyRetentionPolicyToVersionsAsync<TVersion>(
        List<TVersion> versions, 
        string contentId,
        IRepository<TVersion> repository,
        CancellationToken cancellationToken) 
        where TVersion : BaseEntity
    {
        var sortedVersions = versions.OrderByDescending(v => v.CreatedAt).ToList();

        if (sortedVersions.Count <= 10)
        {
            return; // Keep all if we have 10 or fewer
        }

        var toKeep = new HashSet<string>();
        var now = DateTimeOffset.UtcNow;

        // Keep last 10
        foreach (var version in sortedVersions.Take(10))
        {
            toKeep.Add(version.Id);
        }

        // Keep monthly for 2 years
        var twoYearsAgo = now.AddYears(-2);
        var monthlySnapshots = sortedVersions
            .Where(v => v.CreatedAt >= twoYearsAgo)
            .GroupBy(v => new { v.CreatedAt.Year, v.CreatedAt.Month })
            .Select(g => g.First());

        foreach (var version in monthlySnapshots)
        {
            toKeep.Add(version.Id);
        }

        // Keep yearly forever
        var yearlySnapshots = sortedVersions
            .GroupBy(v => v.CreatedAt.Year)
            .Select(g => g.First());

        foreach (var version in yearlySnapshots)
        {
            toKeep.Add(version.Id);
        }

        // Delete versions not in keep set
        var versionsToDelete = sortedVersions.Where(v => !toKeep.Contains(v.Id)).ToList();
        foreach (var version in versionsToDelete)
        {
            await repository.DeleteAsync(version, softDelete: false, cancellationToken); // Hard delete
        }

        if (versionsToDelete.Any())
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Retention policy applied to content {ContentId}: kept {Count} of {Total} versions",
            contentId, toKeep.Count, sortedVersions.Count);
    }

    /// <summary>
    /// Reverts content to a previous version by copying it to Draft.
    /// User must then publish to make it Live.
    /// </summary>
    public virtual async Task RevertToVersionAsync(ISchedulableContent content, string versionId, CancellationToken cancellationToken = default)
    {
        if (content is BlogPost blogPost)
        {
            var versions = await _blogPostVersionRepository.GetVersionsForContentAsync(blogPost.Id, cancellationToken);
            var version = versions.FirstOrDefault(v => v.Id == versionId);

            if (version == null)
            {
                throw new ArgumentException($"Version {versionId} not found", nameof(versionId));
            }

            _logger.LogInformation("Reverting BlogPost {ContentId} to version {VersionNumber}", 
                content.Id, version.VersionNumber);

            var clonedContent = CloneContent(version.Content);
            content.SetDraftContent(clonedContent);
        }
        else if (content is Page page)
        {
            var versions = await _pageVersionRepository.GetVersionsForContentAsync(page.Id, cancellationToken);
            var version = versions.FirstOrDefault(v => v.Id == versionId);

            if (version == null)
            {
                throw new ArgumentException($"Version {versionId} not found", nameof(versionId));
            }

            _logger.LogInformation("Reverting Page {ContentId} to version {VersionNumber}", 
                content.Id, version.VersionNumber);

            var clonedContent = CloneContent(version.Content);
            content.SetDraftContent(clonedContent);
        }
    }

    /// <summary>
    /// Resets Draft to match Live, discarding unpublished changes.
    /// </summary>
    public virtual void ResetDraftToLive(ISchedulableContent content)
    {
        var liveContent = content.GetLiveContent();
        if (liveContent == null)
        {
            _logger.LogWarning("Cannot reset Draft for content {ContentId} - no Live version", content.Id);
            return;
        }

        var clonedContent = CloneContent(liveContent);
        content.SetDraftContent(clonedContent);

        _logger.LogInformation("Draft reset to Live for content {ContentId}", content.Id);
    }

    /// <summary>
    /// Deep clones content with hash computation.
    /// </summary>
    public virtual BaseContent CloneContent(BaseContent source)
    {
        BaseContent clone;

        // Type-specific cloning
        if (source is BlogPostContent blogPost)
        {
            clone = new BlogPostContent
            {
                Short = blogPost.Short
            };
        }
        else if (source is PageContent page)
        {
            clone = new PageContent
            {
                ShowTitle = page.ShowTitle
            };
        }
        else
        {
            clone = new BaseContent();
        }

        // Copy base fields
        clone.Title = source.Title;
        clone.Markdown = source.Markdown;
        clone.Content = source.Content;
        clone.FeaturedImageUrl = source.FeaturedImageUrl;
        clone.FeaturedImageAlt = source.FeaturedImageAlt;
        clone.MetaDescription = source.MetaDescription;
        clone.MetaKeywords = source.MetaKeywords;
        clone.SearchIndex = source.SearchIndex;

        // Compute hash for clone
        clone.ComputeHash();

        return clone;
    }

    /// <summary>
    /// Checks if Draft differs from Live using hash comparison.
    /// </summary>
    public virtual bool DraftDiffersFromLive(ISchedulableContent content)
    {
        return content.DraftDiffersFromLive();
    }

    /// <summary>
    /// Checks if content is published (Live != null).
    /// </summary>
    public virtual bool IsPublished(ISchedulableContent content)
    {
        return content.IsPublished;
    }

    /// <summary>
    /// Clears Live content (unpublish).
    /// </summary>
    public virtual void ClearLive(ISchedulableContent content)
    {
        content.SetLiveContent(null);
    }
}
