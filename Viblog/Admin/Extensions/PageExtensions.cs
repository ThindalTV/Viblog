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
                Draft = new PageContentModel
                {
                    Title = page.Draft.Title,
                    Markdown = page.Draft.Markdown,
                    Content = string.IsNullOrWhiteSpace(page.Draft.Content) ? null : page.Draft.Content,
                    FeaturedImageUrl = page.Draft.FeaturedImageUrl,
                    FeaturedImageAlt = page.Draft.FeaturedImageAlt,
                    MetaDescription = page.Draft.MetaDescription,
                    MetaKeywords = page.Draft.MetaKeywords,
                    ShowTitle = page.Draft.ShowTitle
                },
                
                // Live version
                Live = new PageContentModel
                {
                    Title = page.Live.Title,
                    Markdown = page.Live.Markdown,
                    Content = page.Live.Content,
                    FeaturedImageUrl = page.Live.FeaturedImageUrl,
                    FeaturedImageAlt = page.Live.FeaturedImageAlt,
                    MetaDescription = page.Live.MetaDescription,
                    MetaKeywords = page.Live.MetaKeywords,
                    ShowTitle = page.Live.ShowTitle
                },
                
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
                Draft = new PageContent
                {
                    Title = model.Draft.Title,
                    Markdown = model.Draft.Markdown,
                    Content = model.Draft.Content ?? string.Empty,
                    FeaturedImageUrl = model.Draft.FeaturedImageUrl,
                    FeaturedImageAlt = model.Draft.FeaturedImageAlt,
                    MetaDescription = model.Draft.MetaDescription,
                    MetaKeywords = model.Draft.MetaKeywords,
                    ShowTitle = model.Draft.ShowTitle
                },
                
                // Live version
                Live = new PageContent
                {
                    Title = model.Live.Title,
                    Markdown = model.Live.Markdown,
                    Content = model.Live.Content,
                    FeaturedImageUrl = model.Live.FeaturedImageUrl,
                    FeaturedImageAlt = model.Live.FeaturedImageAlt,
                    MetaDescription = model.Live.MetaDescription,
                    MetaKeywords = model.Live.MetaKeywords,
                    ShowTitle = model.Live.ShowTitle
                },
                
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
