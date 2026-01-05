using Viblog.Frontend.Infrastructure;
using Viblog.Shared.Data.Common;
using Viblog.Shared.Data.Entities;
using Viblog.Shared.Data.Repositories;

namespace Viblog.Frontend.Facades;

/// <summary>
/// Facade implementation for blog archive operations
/// </summary>
public class ArchiveFacade : IArchiveFacade
{
    private readonly IBlogPostRepository _blogPostRepository;

    public ArchiveFacade(IBlogPostRepository blogPostRepository)
    {
        _blogPostRepository = blogPostRepository ?? throw new ArgumentNullException(nameof(blogPostRepository));
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetPostsByMonthAsync(
        int year,
        int month,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        // Return empty result for invalid year or month
        if (year < 1900 || year > 2100 || month < 1 || month > 12)
        {
            return new PagedResult<BlogPost>
            {
                Items = new List<BlogPost>(),
                TotalCount = 0,
                PageNumber = pagingParameters.PageNumber,
                PageSize = pagingParameters.PageSize
            };
        }

        return await _blogPostRepository.GetPostsByMonthAsync(
            year,
            month,
            pagingParameters,
            publishedOnly: true,
            cancellationToken);
    }
}
