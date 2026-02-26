using Viblog.Admin.Models;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;

namespace Viblog.Admin.Extensions;

/// <summary>
/// Extension methods for converting between BlogPost entity and BlogPostModel
/// </summary>
public static class BlogPostExtensions
{
    /// <summary>
    /// Convert BlogPost entity to BlogPostModel
    /// </summary>
    /// <param name="post">The blog post entity</param>
    /// <returns>Blog post view model</returns>
    extension(BlogPost post)
    {
        public BlogPostModel ToModel()
        {
            ArgumentNullException.ThrowIfNull(post);

            return new BlogPostModel
            {
                Id = post.Id,
                PartitionKey = post.GroupKey,
                // Always map Draft content - editors always work on Draft
                Title = post.Draft.Title,
                Slug = post.Slug,
                Short = post.Draft.Short,
                Markdown = post.Draft.Markdown,
                Content = string.IsNullOrWhiteSpace(post.Draft.Content) ? null : post.Draft.Content,
                FeaturedImageUrl = post.Draft.FeaturedImageUrl,
                FeaturedImageAlt = post.Draft.FeaturedImageAlt,
                AuthorName = post.AuthorName,
                AuthorId = post.AuthorId,
                PublishedAt = post.PublishedAt,
                IsPublished = post.IsPublished,  // Computed: Live != null
                IsFeatured = post.IsFeatured,
                Tags = [.. post.Tags],
                CategoryIds = [.. post.CategoryIds]
            };
        }
    }

    /// <summary>
    /// Convert BlogPostModel to BlogPost entity
    /// </summary>
    /// <param name="model">The blog post view model</param>
    /// <returns>Blog post entity</returns>
    extension(BlogPostModel model)
    {
        public BlogPost ToEntity()
        {
            ArgumentNullException.ThrowIfNull(model);

            var post = new BlogPost
            {
                Slug = model.Slug,
                AuthorName = model.AuthorName,
                AuthorId = model.AuthorId,
                PublishedAt = model.PublishedAt,
                // IsPublished is NOT set here — it is computed from Live != null.
                // Live content is managed exclusively by ContentSchedulingService.
                IsFeatured = model.IsFeatured,
                Tags = model.Tags,
                CategoryIds = model.CategoryIds,
                Draft = new BlogPostContent
                {
                    Title = model.Title,
                    Short = model.Short,
                    Markdown = model.Markdown,
                    Content = model.Content ?? string.Empty,
                    FeaturedImageUrl = model.FeaturedImageUrl,
                    FeaturedImageAlt = model.FeaturedImageAlt
                }
            };

            // Preserve existing ID and partition key for updates
            if (!string.IsNullOrWhiteSpace(model.Id))
            {
                post.Id = model.Id;
            }

            return post;
        }
    }
}
