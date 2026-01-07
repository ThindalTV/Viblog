using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Shared.Data;

/// <summary>
/// Application database context configured for CosmosDB
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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
        base.OnModelCreating(builder);

        // Remove indexes that CosmosDB doesn't support
        RemoveIdentityIndexes(builder);

        // Configure Identity entities for CosmosDB
        ConfigureIdentityEntities(builder);

        // Configure blog entities
        ConfigureBlogEntities(builder);
    }

    /// <summary>
    /// Remove default Identity indexes that are not supported by CosmosDB
    /// </summary>
    private static void RemoveIdentityIndexes(ModelBuilder builder)
    {
        // Remove all indexes from all entities (CosmosDB doesn't support EF Core index definitions)
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var indexes = entityType.GetIndexes().ToList();
            foreach (var index in indexes)
            {
                entityType.RemoveIndex(index);
            }
        }
    }

    /// <summary>
    /// Configure Identity entities with partition keys and containers
    /// </summary>
    private static void ConfigureIdentityEntities(ModelBuilder builder)
    {
        // ApplicationUser configuration
        builder.Entity<ApplicationUser>(b =>
        {
            b.ToContainer("Users");
            b.HasPartitionKey(u => u.Id);
            b.Property(u => u.ConcurrencyStamp).IsETagConcurrency();
            b.HasNoDiscriminator();
        });

        // IdentityRole configuration
        builder.Entity<IdentityRole>(b =>
        {
            b.ToContainer("Roles");
            b.HasPartitionKey(r => r.Id);
            b.Property(r => r.ConcurrencyStamp).IsETagConcurrency();
            b.HasNoDiscriminator();
        });

        // IdentityUserClaim configuration
        builder.Entity<IdentityUserClaim<string>>(b =>
        {
            b.ToContainer("UserClaims");
            b.HasPartitionKey(uc => uc.UserId);
            b.HasNoDiscriminator();
        });

        // IdentityUserRole configuration
        builder.Entity<IdentityUserRole<string>>(b =>
        {
            b.ToContainer("UserRoles");
            b.HasPartitionKey(ur => ur.UserId);
            b.HasNoDiscriminator();
        });

        // IdentityUserLogin configuration
        builder.Entity<IdentityUserLogin<string>>(b =>
        {
            b.ToContainer("UserLogins");
            b.HasPartitionKey(ul => ul.UserId);
            b.HasNoDiscriminator();
            b.HasKey(ul => new { ul.LoginProvider, ul.ProviderKey });
        });

        // IdentityRoleClaim configuration
        builder.Entity<IdentityRoleClaim<string>>(b =>
        {
            b.ToContainer("RoleClaims");
            b.HasPartitionKey(rc => rc.RoleId);
            b.HasNoDiscriminator();
        });

        // IdentityUserToken configuration
        builder.Entity<IdentityUserToken<string>>(b =>
        {
            b.ToContainer("UserTokens");
            b.HasPartitionKey(ut => ut.UserId);
            b.HasNoDiscriminator();
            b.HasKey(ut => new { ut.UserId, ut.LoginProvider, ut.Name });
        });
    }

    /// <summary>
    /// Configure blog entities for CosmosDB
    /// </summary>
    private static void ConfigureBlogEntities(ModelBuilder builder)
    {
        builder.Entity<BlogPost>(b =>
        {
            b.ToContainer("BlogPosts");
            b.HasPartitionKey(p => p.PartitionKey);
            b.HasNoDiscriminator();
            
            // Configure list properties
            b.Property(p => p.Tags);
            b.Property(p => p.CategoryIds);
            b.Property(p => p.CategoryNames);
            b.Property(p => p.MediaUrls);
        });

        builder.Entity<MediaItem>(b =>
        {
            b.ToContainer("MediaItems");
            b.HasPartitionKey(m => m.PartitionKey);
            b.HasNoDiscriminator();
            
            // Configure dictionary property for additional metadata
            b.Property(m => m.AdditionalMetadata);
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
