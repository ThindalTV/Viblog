using Viblog.Admin.Models;
using Viblog.Infrastructure.Shared.Data.Entities;

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
                PartitionKey = post.PartitionKey,
                Title = post.Title,
                Slug = post.Slug,
                Short = post.Short,
                Markdown = post.Markdown,
                Content = string.IsNullOrWhiteSpace(post.Content) ? null : post.Content,
                FeaturedImageUrl = post.FeaturedImageUrl,
                FeaturedImageAlt = post.FeaturedImageAlt,
                AuthorName = post.AuthorName,
                AuthorId = post.AuthorId,
                PublishedAt = post.PublishedAt,
                IsPublished = post.IsPublished,
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
                Title = model.Title,
                Slug = model.Slug,
                Short = model.Short,
                Markdown = model.Markdown,
                Content = model.Content ?? string.Empty,
                FeaturedImageUrl = model.FeaturedImageUrl,
                FeaturedImageAlt = model.FeaturedImageAlt,
                AuthorName = model.AuthorName,
                AuthorId = model.AuthorId,
                PublishedAt = model.PublishedAt,
                IsPublished = model.IsPublished,
                IsFeatured = model.IsFeatured,
                Tags = model.Tags,
                CategoryIds = model.CategoryIds
            };

            // Preserve existing ID and partition key for updates
            if (!string.IsNullOrWhiteSpace(model.Id))
            {
                post.Id = model.Id;
                post.PartitionKey = model.PartitionKey ?? post.GetPublicationYear();
            }
            // For new posts, partition key will be set by repository's UpdatePartitionKey() method

            return post;
        }
    }
}
