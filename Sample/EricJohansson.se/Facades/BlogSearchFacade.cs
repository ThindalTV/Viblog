using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Viblog.Infrastructure.Frontend.Facades;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Services;

namespace EricJohansson.se.Facades;

/// <summary>
/// Facade implementation for blog search operations
/// </summary>
public class BlogSearchFacade : IBlogSearchFacade
{
    private readonly IBlogSearchService _blogSearchService;

    public BlogSearchFacade(IBlogSearchService blogSearchService)
    {
        _blogSearchService = blogSearchService ?? throw new ArgumentNullException(nameof(blogSearchService));
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> SearchPostsAsync(
        string searchTerm,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        // Return empty result for invalid search terms
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new PagedResult<BlogPost>
            {
                Items = new List<BlogPost>(),
                TotalCount = 0,
                PageNumber = pagingParameters.PageNumber,
                PageSize = pagingParameters.PageSize
            };
        }

        return await _blogSearchService.SearchAsync(
            searchTerm,
            pagingParameters,
            publishedOnly: true,
            cancellationToken);
    }
}
