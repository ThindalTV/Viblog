using Microsoft.EntityFrameworkCore;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;

namespace Viblog.Data.CosmosDb.Data;

/// <summary>
/// Application database context configured for CosmosDB
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configure entity models and CosmosDB-specific settings
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Configure blog entities
        ConfigureBlogEntities(builder);
    }

    /// <summary>
    /// Configure blog entities for CosmosDB
    /// </summary>
    private static void ConfigureBlogEntities(ModelBuilder builder)
    {
        // AdminUser configuration
        builder.Entity<AdminUser>(b =>
        {
            b.ToContainer("Users");
            b.HasPartitionKey(u => u.GroupKey);
            b.HasNoDiscriminator();

            // Configure custom list properties
            b.Property(u => u.CustomClaims);
        });

        builder.Entity<BlogPost>(b =>
        {
            b.ToContainer("BlogPosts");
            b.HasPartitionKey(p => p.GroupKey);
            b.HasNoDiscriminator();

            b.OwnsOne<BlogPostContent>(p => p.Draft, draft =>
            {
                draft.Property(d => d.Title);
                draft.Property(d => d.Markdown);
                draft.Property(d => d.Content);
                draft.Property(d => d.SearchIndex);
                draft.Property(d => d.Short);
                draft.Property(d => d.FeaturedImageUrl);
                draft.Property(d => d.FeaturedImageAlt);
                draft.Property(d => d.MetaDescription);
                draft.Property(d => d.MetaKeywords);
                draft.Property(d => d.ContentHash);
            });

            b.OwnsOne<BlogPostContent>(p => p.Live, live =>
            {
                live.Property(d => d.Title);
                live.Property(d => d.Markdown);
                live.Property(d => d.Content);
                live.Property(d => d.SearchIndex);
                live.Property(d => d.Short);
                live.Property(d => d.FeaturedImageUrl);
                live.Property(d => d.FeaturedImageAlt);
                live.Property(d => d.MetaDescription);
                live.Property(d => d.MetaKeywords);
                live.Property(d => d.ContentHash);
            });

            // Configure list properties
            b.Property(p => p.Tags);
            b.Property(p => p.CategoryIds);
            b.Property(p => p.CategoryNames);
            b.Property(p => p.MediaUrls);
            b.Property(p => p.IsPublished);
        });

        builder.Entity<Page>(b =>
        {
            b.ToContainer("Pages");
            b.HasPartitionKey(p => p.GroupKey);
            b.HasNoDiscriminator();

            b.OwnsOne<PageContent>(p => p.Draft, draft =>
            {
                draft.Property(d => d.Title);
                draft.Property(d => d.Markdown);
                draft.Property(d => d.Content);
                draft.Property(d => d.SearchIndex);
                draft.Property(d => d.ShowTitle);
                draft.Property(d => d.FeaturedImageUrl);
                draft.Property(d => d.FeaturedImageAlt);
                draft.Property(d => d.MetaDescription);
                draft.Property(d => d.MetaKeywords);
                draft.Property(d => d.ContentHash);
            });

            b.OwnsOne<PageContent>(p => p.Live, live =>
            {
                live.Property(d => d.Title);
                live.Property(d => d.Markdown);
                live.Property(d => d.Content);
                live.Property(d => d.SearchIndex);
                live.Property(d => d.ShowTitle);
                live.Property(d => d.FeaturedImageUrl);
                live.Property(d => d.FeaturedImageAlt);
                live.Property(d => d.MetaDescription);
                live.Property(d => d.MetaKeywords);
                live.Property(d => d.ContentHash);
            });

            b.Property(p => p.IsPublished);
        });

        builder.Entity<BlogPostVersion>(b =>
        {
            b.ToContainer("BlogPostVersions");
            b.HasPartitionKey(v => v.GroupKey);
            b.HasNoDiscriminator();

            b.OwnsOne<BlogPostContent>(v => v.Content, content =>
            {
                content.Property(d => d.Title);
                content.Property(d => d.Markdown);
                content.Property(d => d.Content);
                content.Property(d => d.SearchIndex);
                content.Property(d => d.Short);
                content.Property(d => d.FeaturedImageUrl);
                content.Property(d => d.FeaturedImageAlt);
                content.Property(d => d.MetaDescription);
                content.Property(d => d.MetaKeywords);
                content.Property(d => d.ContentHash);
            });
        });

        builder.Entity<PageVersion>(b =>
        {
            b.ToContainer("PageVersions");
            b.HasPartitionKey(v => v.GroupKey);
            b.HasNoDiscriminator();

            b.OwnsOne<PageContent>(v => v.Content, content =>
            {
                content.Property(d => d.Title);
                content.Property(d => d.Markdown);
                content.Property(d => d.Content);
                content.Property(d => d.SearchIndex);
                content.Property(d => d.ShowTitle);
                content.Property(d => d.FeaturedImageUrl);
                content.Property(d => d.FeaturedImageAlt);
                content.Property(d => d.MetaDescription);
                content.Property(d => d.MetaKeywords);
                content.Property(d => d.ContentHash);
            });
        });

        builder.Entity<MediaItem>(b =>
        {
            b.ToContainer("MediaItems");
            b.HasPartitionKey(m => m.GroupKey);
            b.HasNoDiscriminator();

            // Configure dictionary property for additional metadata
            b.Property(m => m.AdditionalMetadata);
        });

        builder.Entity<AuditLog>(b =>
        {
            b.ToContainer("AuditLogs");
            b.HasPartitionKey(a => a.GroupKey);
            b.HasNoDiscriminator();
        });
    }

    /// <summary>
    /// Override SaveChanges to automatically update timestamps
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Override SaveChanges to automatically update timestamps
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Update timestamps for modified entities
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}
