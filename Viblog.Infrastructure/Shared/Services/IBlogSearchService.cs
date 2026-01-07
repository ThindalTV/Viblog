using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Shared.Services;

/// <summary>
/// Service interface for blog post search operations
/// </summary>
public interface IBlogSearchService
{
    /// <summary>
    /// Search blog posts by term with pagination
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="publishedOnly">Whether to return only published posts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of blog posts matching the search term</returns>
    Task<PagedResult<BlogPost>> SearchAsync(
        string searchTerm,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search blog posts by title only
    /// </summary>
    /// <param name="titleTerm">The title search term</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="publishedOnly">Whether to return only published posts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of blog posts with matching titles</returns>
    Task<PagedResult<BlogPost>> SearchByTitleAsync(
        string titleTerm,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search blog posts by multiple terms (AND logic)
    /// </summary>
    /// <param name="searchTerms">Array of search terms that must all be present</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="publishedOnly">Whether to return only published posts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of blog posts matching all search terms</returns>
    Task<PagedResult<BlogPost>> SearchMultipleTermsAsync(
        string[] searchTerms,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get related posts based on tags and categories
    /// </summary>
    /// <param name="postId">The blog post ID to find related posts for</param>
    /// <param name="partitionKey">The partition key</param>
    /// <param name="maxResults">Maximum number of related posts to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of related blog posts</returns>
    Task<IEnumerable<BlogPost>> GetRelatedPostsAsync(
        string postId,
        string partitionKey,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
}
