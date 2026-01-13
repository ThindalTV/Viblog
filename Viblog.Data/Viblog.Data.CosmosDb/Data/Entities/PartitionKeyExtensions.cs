using System;
using System.Collections.Generic;
using System.Text;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Data.CosmosDb.Data.Entities;

internal static class PartitionKeyExtensions
{
    extension(BaseEntity entity)
    {
        public string SetPartitionKey()
        {
            entity.GroupKey = entity.CreatedAt.Year.ToString();
            return entity.GroupKey;
        }
    }

    extension(BlogPost post)
    {
        public string SetPartitionKey()
        {
            if (post.IsPublished)
            {
                post.GroupKey = post.PublishedAt.Year.ToString();
            }
            else
            {
                post.GroupKey = "draft";
            }

            return post.GroupKey;
        }
    }

    extension(MediaItem mediaItem)
    {
        public string SetPartitionKey() => ((BaseEntity)mediaItem).SetPartitionKey();
    }

    extension(Page page)
    {
        public string SetPartitionKey()
        {
            // Pages use a simple "pages" partition key since they're static and not time-based
            page.GroupKey = "pages";
            return page.GroupKey;
        }
    }
}