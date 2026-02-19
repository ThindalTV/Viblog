using Microsoft.EntityFrameworkCore;
using Viblog.Infrastructure.Shared.Data.Entities;

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
        // ApplicationUser configuration
        builder.Entity<ApplicationUser>(b =>
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

            // Configure list properties
            b.Property(p => p.Tags);
            b.Property(p => p.CategoryIds);
            b.Property(p => p.CategoryNames);
            b.Property(p => p.MediaUrls);
        });

        builder.Entity<Page>(b =>
        {
            b.ToContainer("Pages");
            b.HasPartitionKey(p => p.GroupKey);
            b.HasNoDiscriminator();
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
