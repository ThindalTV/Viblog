using Moq;
using Vilog.Frontend.Facades;
using Vilog.Shared.Data.Entities;
using Vilog.Shared.Data.Repositories;

namespace Vilog.Tests.Facades;

/// <summary>
/// Unit tests for CommentFacade
/// </summary>
public class CommentFacadeTests
{
    private readonly Mock<IBlogPostRepository> _mockRepository;
    private readonly CommentFacade _facade;

    public CommentFacadeTests()
    {
        _mockRepository = new Mock<IBlogPostRepository>();
        _facade = new CommentFacade(_mockRepository.Object);
    }

    [Fact]
    public async Task GetApprovedCommentsAsync_WhenSlugIsEmpty_ReturnsEmptyCollection()
    {
        // Act
        var result = await _facade.GetApprovedCommentsAsync(string.Empty);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApprovedCommentsAsync_WhenPostNotFound_ReturnsEmptyCollection()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetBySlugAsync(It.IsAny<string>(), true, default))
            .ReturnsAsync((BlogPost?)null);

        // Act
        var result = await _facade.GetApprovedCommentsAsync("nonexistent-slug");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApprovedCommentsAsync_WhenPostHasComments_ReturnsComments()
    {
        // Arrange
        var post = CreateTestPost();
        var comments = new List<Comment>
        {
            CreateTestComment("1", "Author 1"),
            CreateTestComment("2", "Author 2")
        };

        _mockRepository.Setup(r => r.GetBySlugAsync("test-slug", true, default))
            .ReturnsAsync(post);
        _mockRepository.Setup(r => r.GetApprovedCommentsAsync(post.Id, post.PartitionKey, default))
            .ReturnsAsync(comments);

        // Act
        var result = await _facade.GetApprovedCommentsAsync("test-slug");

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task AddCommentAsync_WhenParametersInvalid_ReturnsFalse()
    {
        // Act - empty slug
        var result1 = await _facade.AddCommentAsync(string.Empty, "Name", "email@test.com", null, "Content", null, null, null, null);
        
        // Act - empty name
        var result2 = await _facade.AddCommentAsync("slug", string.Empty, "email@test.com", null, "Content", null, null, null, null);
        
        // Act - empty email
        var result3 = await _facade.AddCommentAsync("slug", "Name", string.Empty, null, "Content", null, null, null, null);
        
        // Act - empty content
        var result4 = await _facade.AddCommentAsync("slug", "Name", "email@test.com", null, string.Empty, null, null, null, null);

        // Assert
        Assert.False(result1);
        Assert.False(result2);
        Assert.False(result3);
        Assert.False(result4);
    }

    [Fact]
    public async Task AddCommentAsync_WhenPostNotFound_ReturnsFalse()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetBySlugAsync(It.IsAny<string>(), true, default))
            .ReturnsAsync((BlogPost?)null);

        // Act
        var result = await _facade.AddCommentAsync("slug", "Name", "email@test.com", null, "Content", null, null, null, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddCommentAsync_WhenCommentsNotAllowed_ReturnsFalse()
    {
        // Arrange
        var post = CreateTestPost();
        post.AllowComments = false;

        _mockRepository.Setup(r => r.GetBySlugAsync("test-slug", true, default))
            .ReturnsAsync(post);

        // Act
        var result = await _facade.AddCommentAsync("test-slug", "Name", "email@test.com", null, "Content", null, null, null, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddCommentAsync_WhenValid_ReturnsTrue()
    {
        // Arrange
        var post = CreateTestPost();
        
        _mockRepository.Setup(r => r.GetBySlugAsync("test-slug", true, default))
            .ReturnsAsync(post);
        _mockRepository.Setup(r => r.AddCommentAsync(post.Id, post.PartitionKey, It.IsAny<Comment>(), default))
            .ReturnsAsync(post);

        // Act
        var result = await _facade.AddCommentAsync("test-slug", "John Doe", "john@test.com", "https://john.com", "Great post!", null, "127.0.0.1", "Mozilla", "user-123");

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.AddCommentAsync(
            post.Id, 
            post.PartitionKey, 
            It.Is<Comment>(c => 
                c.AuthorName == "John Doe" && 
                c.AuthorEmail == "john@test.com" &&
                c.Content == "Great post!" &&
                !c.IsApproved), // Should be false by default
            default), Times.Once);
    }

    [Fact]
    public async Task UpdateCommentAsync_WhenParametersInvalid_ReturnsFalse()
    {
        // Act
        var result1 = await _facade.UpdateCommentAsync(string.Empty, "comment-id", "content", "user-id");
        var result2 = await _facade.UpdateCommentAsync("slug", string.Empty, "content", "user-id");
        var result3 = await _facade.UpdateCommentAsync("slug", "comment-id", string.Empty, "user-id");

        // Assert
        Assert.False(result1);
        Assert.False(result2);
        Assert.False(result3);
    }

    [Fact]
    public async Task DeleteCommentAsync_WhenParametersInvalid_ReturnsFalse()
    {
        // Act
        var result1 = await _facade.DeleteCommentAsync(string.Empty, "comment-id", "user-id");
        var result2 = await _facade.DeleteCommentAsync("slug", string.Empty, "user-id");

        // Assert
        Assert.False(result1);
        Assert.False(result2);
    }

    [Fact]
    public async Task ApproveCommentAsync_WhenValid_ReturnsTrue()
    {
        // Arrange
        var post = CreateTestPost();
        
        _mockRepository.Setup(r => r.GetBySlugAsync("test-slug", false, default))
            .ReturnsAsync(post);
        _mockRepository.Setup(r => r.ApproveCommentAsync(post.Id, post.PartitionKey, "comment-id", default))
            .ReturnsAsync(post);

        // Act
        var result = await _facade.ApproveCommentAsync("test-slug", "comment-id");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task MarkCommentAsSpamAsync_WhenValid_ReturnsTrue()
    {
        // Arrange
        var post = CreateTestPost();
        
        _mockRepository.Setup(r => r.GetBySlugAsync("test-slug", false, default))
            .ReturnsAsync(post);
        _mockRepository.Setup(r => r.MarkCommentAsSpamAsync(post.Id, post.PartitionKey, "comment-id", default))
            .ReturnsAsync(post);

        // Act
        var result = await _facade.MarkCommentAsSpamAsync("test-slug", "comment-id");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetApprovedCommentsAsync_WithThreadedComments_BuildsHierarchy()
    {
        // Arrange
        var post = CreateTestPost();
        var comment1 = CreateTestComment("1", "Author 1");
        var comment2 = CreateTestComment("2", "Author 2", "1"); // Reply to comment 1
        var comment3 = CreateTestComment("3", "Author 3");
        
        var comments = new List<Comment> { comment1, comment2, comment3 };

        _mockRepository.Setup(r => r.GetBySlugAsync("test-slug", true, default))
            .ReturnsAsync(post);
        _mockRepository.Setup(r => r.GetApprovedCommentsAsync(post.Id, post.PartitionKey, default))
            .ReturnsAsync(comments);

        // Act
        var result = await _facade.GetApprovedCommentsAsync("test-slug");
        var resultList = result.ToList();

        // Assert
        Assert.Equal(2, resultList.Count); // Only 2 root comments
        Assert.Single(resultList[0].Replies); // First comment has 1 reply
        Assert.Empty(resultList[1].Replies); // Third comment has no replies
    }

    private static BlogPost CreateTestPost()
    {
        return new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = "test",
            Slug = "test-slug",
            Title = "Test Post",
            AllowComments = true
        };
    }

    private static Comment CreateTestComment(string id, string authorName, string? parentId = null)
    {
        return new Comment
        {
            Id = id,
            AuthorName = authorName,
            AuthorEmail = $"{authorName.ToLower().Replace(" ", "")}@test.com",
            Content = $"Comment by {authorName}",
            IsApproved = true,
            ParentCommentId = parentId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
