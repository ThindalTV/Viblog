using Viblog.Infrastructure.Extensions;
using Viblog.Infrastructure.Data.Entities.Content;

namespace Viblog.Tests.Services;

/// <summary>
/// Unit tests for SchedulableContentExtensions and related entity-state computed properties.
/// </summary>
public class SchedulableContentExtensionsTests
{
    #region IsPublished

    [Fact]
    public void IsPublished_BlogPost_WhenLiveIsNull_ReturnsFalse()
    {
        var post = new BlogPost { Live = null };

        Assert.False(post.IsPublished);
    }

    [Fact]
    public void IsPublished_BlogPost_WhenLiveIsSet_ReturnsTrue()
    {
        var post = new BlogPost();
        post.SetLiveContent(new BlogPostContent { Title = "Live" });

        Assert.True(post.IsPublished);
    }

    [Fact]
    public void IsPublished_Page_WhenLiveIsNull_ReturnsFalse()
    {
        var page = new Page { Live = null };

        Assert.False(page.IsPublished);
    }

    [Fact]
    public void IsPublished_Page_WhenLiveIsSet_ReturnsTrue()
    {
        var page = new Page();
        page.SetLiveContent(new PageContent { Title = "Live" });

        Assert.True(page.IsPublished);
    }

    #endregion

    #region GetLiveContent

    [Fact]
    public void GetLiveContent_BlogPost_ReturnsLiveContent()
    {
        var live = new BlogPostContent { Title = "Live" };
        var post = new BlogPost { Live = live };

        var result = post.GetLiveContent();

        Assert.Same(live, result);
    }

    [Fact]
    public void GetLiveContent_Page_ReturnsLiveContent()
    {
        var live = new PageContent { Title = "Live" };
        var page = new Page { Live = live };

        var result = page.GetLiveContent();

        Assert.Same(live, result);
    }

    [Fact]
    public void GetLiveContent_WhenNotPublished_ReturnsNull()
    {
        var post = new BlogPost { Live = null };

        Assert.Null(post.GetLiveContent());
    }

    #endregion

    #region IsScheduled

    [Fact]
    public void IsScheduled_WhenStatusScheduledAndDateSet_ReturnsTrue()
    {
        var post = new BlogPost();
        post.Schedule.Status = ContentStatus.Scheduled;
        post.Schedule.ScheduledPublishDate = DateTimeOffset.UtcNow.AddDays(1);

        Assert.True(post.IsScheduled());
    }

    [Fact]
    public void IsScheduled_WhenStatusDraft_ReturnsFalse()
    {
        var post = new BlogPost();
        post.Schedule.Status = ContentStatus.Draft;

        Assert.False(post.IsScheduled());
    }

    [Fact]
    public void IsScheduled_WhenStatusScheduledButNoDate_ReturnsFalse()
    {
        var post = new BlogPost();
        post.Schedule.Status = ContentStatus.Scheduled;
        post.Schedule.ScheduledPublishDate = null;

        Assert.False(post.IsScheduled());
    }

    #endregion

    #region IsReadyToPublish

    [Fact]
    public void IsReadyToPublish_WhenScheduledDateInPast_ReturnsTrue()
    {
        var post = new BlogPost();
        post.Schedule.Status = ContentStatus.Scheduled;
        post.Schedule.ScheduledPublishDate = DateTimeOffset.UtcNow.AddSeconds(-1);

        Assert.True(post.IsReadyToPublish());
    }

    [Fact]
    public void IsReadyToPublish_WhenScheduledDateInFuture_ReturnsFalse()
    {
        var post = new BlogPost();
        post.Schedule.Status = ContentStatus.Scheduled;
        post.Schedule.ScheduledPublishDate = DateTimeOffset.UtcNow.AddDays(1);

        Assert.False(post.IsReadyToPublish());
    }

    [Fact]
    public void IsReadyToPublish_WhenNotScheduled_ReturnsFalse()
    {
        var post = new BlogPost();
        post.Schedule.Status = ContentStatus.Draft;

        Assert.False(post.IsReadyToPublish());
    }

    #endregion

    #region DraftDiffersFromLive

    [Fact]
    public void DraftDiffersFromLive_WhenNeverPublished_ReturnsTrue()
    {
        var post = new BlogPost
        {
            Draft = new BlogPostContent { Title = "Draft", Markdown = "Content" },
            Live = null
        };

        Assert.True(post.DraftDiffersFromLive());
    }

    [Fact]
    public void DraftDiffersFromLive_WhenDraftMatchesLive_ReturnsFalse()
    {
        var content = new BlogPostContent { Title = "Same", Markdown = "Same content" };
        content.ComputeHash();

        var draftCopy = new BlogPostContent { Title = "Same", Markdown = "Same content" };
        draftCopy.ComputeHash();

        var post = new BlogPost { Draft = draftCopy, Live = content };

        Assert.False(post.DraftDiffersFromLive());
    }

    [Fact]
    public void DraftDiffersFromLive_WhenDraftDiffersFromLive_ReturnsTrue()
    {
        var post = new BlogPost
        {
            Draft = new BlogPostContent { Title = "Updated Draft", Markdown = "New content" },
            Live = new BlogPostContent { Title = "Original", Markdown = "Old content" }
        };

        Assert.True(post.DraftDiffersFromLive());
    }

    #endregion

    #region HasPendingUpdate (entity property)

    [Fact]
    public void HasPendingUpdate_WhenPublishedAndScheduled_ReturnsTrue()
    {
        var post = new BlogPost
        {
            Live = new BlogPostContent { Title = "Live" }
        };
        post.Schedule.Status = ContentStatus.Scheduled;
        post.Schedule.ScheduledPublishDate = DateTimeOffset.UtcNow.AddDays(1);

        Assert.True(post.HasPendingUpdate);
    }

    [Fact]
    public void HasPendingUpdate_WhenPublishedButNotScheduled_ReturnsFalse()
    {
        var post = new BlogPost
        {
            Live = new BlogPostContent { Title = "Live" }
        };
        post.Schedule.Status = ContentStatus.Draft;

        Assert.False(post.HasPendingUpdate);
    }

    [Fact]
    public void HasPendingUpdate_WhenNotPublished_ReturnsFalse()
    {
        var post = new BlogPost { Live = null };
        post.Schedule.Status = ContentStatus.Scheduled;
        post.Schedule.ScheduledPublishDate = DateTimeOffset.UtcNow.AddDays(1);

        Assert.False(post.HasPendingUpdate);
    }

    [Fact]
    public void HasPendingUpdate_Page_WhenPublishedAndScheduled_ReturnsTrue()
    {
        var page = new Page
        {
            Live = new PageContent { Title = "Live" }
        };
        page.Schedule.Status = ContentStatus.Scheduled;
        page.Schedule.ScheduledPublishDate = DateTimeOffset.UtcNow.AddDays(1);

        Assert.True(page.HasPendingUpdate);
    }

    #endregion
}
