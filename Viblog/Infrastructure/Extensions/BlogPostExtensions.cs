using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Entities.Content;

namespace Viblog.Infrastructure.Extensions;

public static class BlogPostExtensions
{
    extension(BlogPost post)
    {
        public bool HasPendingUpdate => post.Live != null
            && post.Schedule.Status == ContentStatus.Scheduled;
    }
}
