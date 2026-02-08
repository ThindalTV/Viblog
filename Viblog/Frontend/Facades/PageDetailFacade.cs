using Viblog.Infrastructure.Frontend.Facades;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Frontend.Facades;

/// <summary>
/// Facade implementation for page detail operations (public-facing)
/// </summary>
public class PageDetailFacade : IPageDetailFacade
{
    private readonly IPageRepository _pageRepository;

    public PageDetailFacade(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository ?? throw new ArgumentNullException(nameof(pageRepository));
    }

    /// <inheritdoc/>
    public virtual async Task<Page?> GetPageBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        // Return null for invalid slug
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return await _pageRepository.GetBySlugAsync(
            slug,
            publishedOnly: true,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task IncrementViewCountAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        await _pageRepository.IncrementViewCountAsync(id, partitionKey, cancellationToken);
    }
}
