using Vilog.Frontend.Infrastructure;
using Vilog.Shared.Data.Common;
using Vilog.Shared.Data.Entities;
using Vilog.Shared.Data.Repositories;

namespace Vilog.Frontend.Facades;

/// <summary>
/// Facade implementation for category-filtered blog post operations
/// </summary>
public class CategoryPostsFacade : ICategoryPostsFacade
{
    private readonly IBlogPostRepository _blogPostRepository;

    public CategoryPostsFacade(IBlogPostRepository blogPostRepository)
    {
        _blogPostRepository = blogPostRepository ?? throw new ArgumentNullException(nameof(blogPostRepository));
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetPostsByCategoryAsync(
        string categoryId,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        // Return empty result for invalid category ID
        if (string.IsNullOrWhiteSpace(categoryId))
        {
            return new PagedResult<BlogPost>
            {
                Items = new List<BlogPost>(),
                TotalCount = 0,
                PageNumber = pagingParameters.PageNumber,
                PageSize = pagingParameters.PageSize
            };
        }

        return await _blogPostRepository.GetPostsByCategoryAsync(
            categoryId,
            pagingParameters,
            publishedOnly: true,
            cancellationToken);
    }
}
