using EricJohansson.se.Infrastructure.Facades;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Repositories;

namespace EricJohansson.se.Facades;

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
