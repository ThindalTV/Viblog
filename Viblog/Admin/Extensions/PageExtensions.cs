using Viblog.Admin.Models;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Admin.Extensions;

/// <summary>
/// Extension methods for converting between Page entity and PageModel
/// </summary>
public static class PageExtensions
{
    /// <summary>
    /// Convert Page entity to PageModel
    /// </summary>
    /// <param name="page">The page entity</param>
    /// <returns>Page view model</returns>
    extension(Page page)
    {
        public PageModel ToModel()
        {
            ArgumentNullException.ThrowIfNull(page);

            return new PageModel
            {
                Id = page.Id,
                PartitionKey = page.GroupKey,
                Slug = page.Slug,
                IsPublished = page.IsPublished,
                PublishDate = page.PublishDate,
                
                // Draft version
                DraftTitle = page.DraftTitle,
                DraftMarkdown = page.DraftMarkdown,
                DraftContent = string.IsNullOrWhiteSpace(page.DraftContent) ? null : page.DraftContent,
                DraftFeaturedImageUrl = page.DraftFeaturedImageUrl,
                DraftFeaturedImageAlt = page.DraftFeaturedImageAlt,
                DraftMetaDescription = page.DraftMetaDescription,
                DraftMetaKeywords = page.DraftMetaKeywords,
                
                // Live version
                LiveTitle = page.LiveTitle,
                LiveMarkdown = page.LiveMarkdown,
                LiveContent = page.LiveContent,
                LiveFeaturedImageUrl = page.LiveFeaturedImageUrl,
                LiveFeaturedImageAlt = page.LiveFeaturedImageAlt,
                LiveMetaDescription = page.LiveMetaDescription,
                LiveMetaKeywords = page.LiveMetaKeywords,
                
                // Common
                AuthorName = page.AuthorName,
                AuthorId = page.AuthorId,
                ViewCount = page.ViewCount
            };
        }
    }

    /// <summary>
    /// Convert PageModel to Page entity
    /// </summary>
    /// <param name="model">The page view model</param>
    /// <returns>Page entity</returns>
    extension(PageModel model)
    {
        public Page ToEntity()
        {
            ArgumentNullException.ThrowIfNull(model);

            var page = new Page
            {
                Slug = model.Slug,
                IsPublished = model.IsPublished,
                PublishDate = model.PublishDate,
                
                // Draft version
                DraftTitle = model.DraftTitle,
                DraftMarkdown = model.DraftMarkdown,
                DraftContent = model.DraftContent ?? string.Empty,
                DraftFeaturedImageUrl = model.DraftFeaturedImageUrl,
                DraftFeaturedImageAlt = model.DraftFeaturedImageAlt,
                DraftMetaDescription = model.DraftMetaDescription,
                DraftMetaKeywords = model.DraftMetaKeywords,
                
                // Live version
                LiveTitle = model.LiveTitle,
                LiveMarkdown = model.LiveMarkdown,
                LiveContent = model.LiveContent,
                LiveFeaturedImageUrl = model.LiveFeaturedImageUrl,
                LiveFeaturedImageAlt = model.LiveFeaturedImageAlt,
                LiveMetaDescription = model.LiveMetaDescription,
                LiveMetaKeywords = model.LiveMetaKeywords,
                
                // Common
                AuthorName = model.AuthorName,
                AuthorId = model.AuthorId,
                ViewCount = model.ViewCount
            };

            // Preserve existing ID and partition key for updates
            if (!string.IsNullOrWhiteSpace(model.Id))
            {
                page.Id = model.Id;
            }

            return page;
        }
    }
}
