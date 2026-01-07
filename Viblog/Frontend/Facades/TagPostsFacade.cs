using Viblog.Infrastructure.Frontend.Facades;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Frontend.Facades;

/// <summary>
/// Facade implementation for tag-filtered blog post operations
/// </summary>
public class TagPostsFacade : ITagPostsFacade
{
    private readonly IBlogPostRepository _blogPostRepository;

    public TagPostsFacade(IBlogPostRepository blogPostRepository)
    {
        _blogPostRepository = blogPostRepository ?? throw new ArgumentNullException(nameof(blogPostRepository));
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetPostsByTagAsync(
        string tag,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        // Return empty result for invalid tag
        if (string.IsNullOrWhiteSpace(tag))
        {
            return new PagedResult<BlogPost>
            {
                Items = new List<BlogPost>(),
                TotalCount = 0,
                PageNumber = pagingParameters.PageNumber,
                PageSize = pagingParameters.PageSize
            };
        }

        return await _blogPostRepository.GetPostsByTagAsync(
            tag,
            pagingParameters,
            publishedOnly: true,
            cancellationToken);
    }
}
