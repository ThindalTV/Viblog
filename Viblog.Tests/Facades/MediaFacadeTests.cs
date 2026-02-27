using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Viblog.Tests.Facades;

/// <summary>
/// Comprehensive unit tests for MediaFacade
/// </summary>
public class MediaFacadeTests
{
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly Mock<IMediaMetadataRepository> _mockMetadataRepository;
    private readonly Mock<ILogger<MediaFacade>> _mockLogger;
    private readonly MediaFacade _facade;

    public MediaFacadeTests()
    {
        _mockMediaService = new Mock<IMediaService>();
        _mockMetadataRepository = new Mock<IMediaMetadataRepository>();
        _mockLogger = new Mock<ILogger<MediaFacade>>();

        _facade = new MediaFacade(
            _mockMediaService.Object,
            _mockMetadataRepository.Object,
            _mockLogger.Object);
    }

    #region BulkUploadAsync Tests

    [Fact]
    public async Task BulkUploadAsync_WithMultipleFiles_UploadsAll()
    {
        // Arrange
        var files = new List<(string FileName, Stream FileStream, string MimeType)>
        {
            ("file1.jpg", new MemoryStream(new byte[] { 1, 2, 3 }), "image/jpeg"),
            ("file2.png", new MemoryStream(new byte[] { 4, 5, 6 }), "image/png"),
            ("file3.pdf", new MemoryStream(new byte[] { 7, 8, 9 }), "application/pdf")
        };

        var folderPath = "/uploads";
        var uploadedBy = "test-user";

        // Setup specific returns for each file
        _mockMediaService
            .Setup(x => x.UploadAsync(
                "file1.jpg",
                It.IsAny<Stream>(),
                "image/jpeg",
                folderPath,
                uploadedBy,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaItem { FileName = "file1.jpg", MimeType = "image/jpeg" });

        _mockMediaService
            .Setup(x => x.UploadAsync(
                "file2.png",
                It.IsAny<Stream>(),
                "image/png",
                folderPath,
                uploadedBy,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaItem { FileName = "file2.png", MimeType = "image/png" });

        _mockMediaService
            .Setup(x => x.UploadAsync(
                "file3.pdf",
                It.IsAny<Stream>(),
                "application/pdf",
                folderPath,
                uploadedBy,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaItem { FileName = "file3.pdf", MimeType = "application/pdf" });

        // Act
        var result = await _facade.BulkUploadAsync(files, folderPath, uploadedBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("file1.jpg", result[0].FileName);
        Assert.Equal("file2.png", result[1].FileName);
        Assert.Equal("file3.pdf", result[2].FileName);
        
        // Verify each file was uploaded with correct parameters
        _mockMediaService.Verify(x => x.UploadAsync(
            "file1.jpg",
            It.IsAny<Stream>(),
            "image/jpeg",
            folderPath,
            uploadedBy,
            It.IsAny<CancellationToken>()), Times.Once);
            
        _mockMediaService.Verify(x => x.UploadAsync(
            "file2.png",
            It.IsAny<Stream>(),
            "image/png",
            folderPath,
            uploadedBy,
            It.IsAny<CancellationToken>()), Times.Once);
            
        _mockMediaService.Verify(x => x.UploadAsync(
            "file3.pdf",
            It.IsAny<Stream>(),
            "application/pdf",
            folderPath,
            uploadedBy,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetMediaItemsAsync Tests

    [Fact]
    public async Task GetMediaItemsAsync_WithMimeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var mimeTypeFilter = "image/*";
        var paging = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedItems = new List<MediaItem>
        {
            new() { FileName = "test1.jpg", MimeType = "image/jpeg" },
            new() { FileName = "test2.png", MimeType = "image/png" }
        };
        var expectedResult = new PagedResult<MediaItem> 
        { 
            Items = expectedItems, 
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _mockMetadataRepository
            .Setup(x => x.GetItemsByTypeAsync(mimeTypeFilter, paging, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetMediaItemsAsync(mimeTypeFilter, paging);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.ToList().Count);
        Assert.Equal("test1.jpg", result.Items.First().FileName);
        Assert.Equal("test2.png", result.Items.Last().FileName);
        
        _mockMetadataRepository.Verify(x => x.GetItemsByTypeAsync(
            mimeTypeFilter,
            paging,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMediaItemsAsync_WithoutFilters_ReturnsAllResults()
    {
        // Arrange
        var paging = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedItems = new List<MediaItem>
        {
            new() { FileName = "file1.jpg", MimeType = "image/jpeg" },
            new() { FileName = "file2.pdf", MimeType = "application/pdf" }
        };
        var expectedResult = new PagedResult<MediaItem> 
        { 
            Items = expectedItems, 
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _mockMetadataRepository
            .Setup(x => x.GetAllAsync<DateTimeOffset>(
                It.IsAny<PagingParameters>(),
                It.IsAny<Expression<Func<MediaItem, DateTimeOffset>>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetMediaItemsAsync(null, paging);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal("file1.jpg", result.Items.First().FileName);
        Assert.Equal("file2.pdf", result.Items.Last().FileName);

        _mockMetadataRepository.Verify(x => x.GetAllAsync<DateTimeOffset>(
            It.IsAny<PagingParameters>(),
            It.IsAny<Expression<Func<MediaItem, DateTimeOffset>>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var id = "test-id";
        var partitionKey = "test-key";

        _mockMediaService
            .Setup(x => x.DeleteAsync(id, partitionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _facade.DeleteAsync(id, partitionKey);

        // Assert
        Assert.True(result);
        _mockMediaService.Verify(x => x.DeleteAsync(
            id, 
            partitionKey, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var id = "invalid-id";
        var partitionKey = "test-key";

        _mockMediaService
            .Setup(x => x.DeleteAsync(id, partitionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _facade.DeleteAsync(id, partitionKey);

        // Assert
        Assert.False(result);
        _mockMediaService.Verify(x => x.DeleteAsync(
            id, 
            partitionKey, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region BulkDeleteAsync Tests

    [Fact]
    public async Task BulkDeleteAsync_WithMultipleItems_DeletesAll()
    {
        // Arrange
        var items = new List<MediaItem>
        {
            new() { Id = "id1", GroupKey = "key1" },
            new() { Id = "id2", GroupKey = "key2" },
            new() { Id = "id3", GroupKey = "key3" }
        };

        _mockMediaService
            .Setup(x => x.DeleteAsync("id1", "key1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        _mockMediaService
            .Setup(x => x.DeleteAsync("id2", "key2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        _mockMediaService
            .Setup(x => x.DeleteAsync("id3", "key3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _facade.BulkDeleteAsync(items);

        // Assert
        Assert.Equal(3, result);
        
        // Verify each item was deleted with specific IDs
        _mockMediaService.Verify(x => x.DeleteAsync(
            "id1",
            "key1",
            It.IsAny<CancellationToken>()), Times.Once);
            
        _mockMediaService.Verify(x => x.DeleteAsync(
            "id2",
            "key2",
            It.IsAny<CancellationToken>()), Times.Once);
            
        _mockMediaService.Verify(x => x.DeleteAsync(
            "id3",
            "key3",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkDeleteAsync_WithPartialFailure_ReturnsSuccessCount()
    {
        // Arrange
        var items = new List<MediaItem>
        {
            new() { Id = "id1", GroupKey = "key1" },
            new() { Id = "id2", GroupKey = "key2" },
            new() { Id = "id3", GroupKey = "key3" }
        };

        _mockMediaService
            .Setup(x => x.DeleteAsync("id1", "key1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        _mockMediaService
            .Setup(x => x.DeleteAsync("id2", "key2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // This one fails
            
        _mockMediaService
            .Setup(x => x.DeleteAsync("id3", "key3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _facade.BulkDeleteAsync(items);

        // Assert
        Assert.Equal(2, result); // Only 2 succeeded
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithSearchTerm_ReturnsMatchingResults()
    {
        // Arrange
        var searchTerm = "vacation";
        var paging = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedItems = new List<MediaItem>
        {
            new() { FileName = "vacation2023.jpg", Title = "Summer Vacation", MimeType = "image/jpeg" }
        };
        var expectedResult = new PagedResult<MediaItem> 
        { 
            Items = expectedItems, 
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockMetadataRepository
            .Setup(x => x.SearchAsync(searchTerm, paging, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.SearchAsync(searchTerm, paging);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("vacation2023.jpg", result.Items.First().FileName);
        Assert.Equal("Summer Vacation", result.Items.First().Title);
        
        _mockMetadataRepository.Verify(x => x.SearchAsync(
            searchTerm, 
            paging, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithNoMatches_ReturnsEmptyResult()
    {
        // Arrange
        var searchTerm = "nonexistent";
        var paging = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<MediaItem> 
        { 
            Items = new List<MediaItem>(), 
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _mockMetadataRepository
            .Setup(x => x.SearchAsync(searchTerm, paging, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.SearchAsync(searchTerm, paging);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
        
        _mockMetadataRepository.Verify(x => x.SearchAsync(
            searchTerm, 
            paging, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // Note: Tests for folder-based operations (GetItemsInFolderAsync, MoveToFolderAsync,
    // BulkMoveAsync, GetAllFolderPathsAsync) have been removed as these operations are
    // no longer supported with the date-based automatic folder structure.
}
