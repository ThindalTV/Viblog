using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;

namespace Viblog.Infrastructure.Shared.Extensions;

public static class PageExtensions
{
    extension(Page page)
    {
        /// <summary>True when published Live content exists but Draft has a scheduled update pending.</summary>
        public bool HasPendingUpdate => page.Live != null
            && page.Schedule.Status == ContentStatus.Scheduled;
    }
}
