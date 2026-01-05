using Vilog.Frontend.Facades;
using Vilog.Frontend.Infrastructure;

namespace Vilog.Frontend;

public static class RegisterFrontendExtensions
{
    /// <summary>
    /// Adds frontend services to the service collection
    /// </summary>
    extension(IServiceCollection collection)
    {
        public IServiceCollection AddVilogFrontend()
        {
            // Register facades
            collection.AddScoped<IFrontPageFacade, FrontPageFacade>();
            collection.AddScoped<IBlogPostListFacade, BlogPostListFacade>();
            collection.AddScoped<IBlogPostDetailFacade, BlogPostDetailFacade>();
            collection.AddScoped<ICategoryPostsFacade, CategoryPostsFacade>();
            collection.AddScoped<ITagPostsFacade, TagPostsFacade>();
            collection.AddScoped<IBlogSearchFacade, BlogSearchFacade>();
            collection.AddScoped<IFeedFacade, FeedFacade>();
            collection.AddScoped<IArchiveFacade, ArchiveFacade>();
            collection.AddScoped<ICommentFacade, CommentFacade>();

            return collection;
        }
    }

    /// <summary>
    /// Adds frontend middleware to the application pipeline (currently no-op for future extensibility)
    /// </summary>
    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseVilogFrontend()
        {
            // Reserved for future frontend-specific middleware
            // (e.g., custom response caching, frontend-specific error handling)
            return app;
        }
    }
}
