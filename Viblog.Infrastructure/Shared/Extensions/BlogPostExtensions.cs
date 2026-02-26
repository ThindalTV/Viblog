using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;

namespace Viblog.Infrastructure.Shared.Extensions;

public static class BlogPostExtensions
{
    extension(BlogPost post)
    {
        public bool HasPendingUpdate => post.Live != null
            && post.Schedule.Status == ContentStatus.Scheduled;
    }
}
