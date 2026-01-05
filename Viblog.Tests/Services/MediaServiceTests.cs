using Microsoft.Extensions.Logging;
using Moq;
using Viblog.Shared.Data.Entities;
using Viblog.Shared.Data.Repositories;
using Viblog.Shared.Services;
using Xunit;

namespace Viblog.Tests.Services;

/// <summary>
/// Comprehensive unit tests for MediaService
/// </summary>
public class MediaServiceTests
{
    private readonly Mock<IMediaStorageRepository> _mockStorageRepository;
    private readonly Mock<IMediaMetadataRepository> _mockMetadataRepository;
    private readonly Mock<IMetadataExtractorService> _mockMetadataExtractor;
    private readonly Mock<ILogger<MediaService>> _mockLogger;
    private readonly MediaService _service;

    public MediaServiceTests()
    {
        _mockStorageRepository = new Mock<IMediaStorageRepository>();
        _mockMetadataRepository = new Mock<IMediaMetadataRepository>();
        _mockMetadataExtractor = new Mock<IMetadataExtractorService>();
        _mockLogger = new Mock<ILogger<MediaService>>();

        _service = new MediaService(
            _mockStorageRepository.Object,
            _mockMetadataRepository.Object,
            _mockMetadataExtractor.Object,
            _mockLogger.Object);
    }

    #region UploadAsync Tests

    [Fact]
    public async Task UploadAsync_WithValidFile_CreatesMediaItem()
    {
        // Arrange
        var fileName = "test-image.jpg";
        var mimeType = "image/jpeg";
        var folderPath = "/images";
        var uploadedBy = "test-user";
        using var fileStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // JPEG header

        var storageResult = new MediaStorageResult
        {
            StoragePath = "2024/01/test-image.jpg",
            PublicUrl = "https://cdn.example.com/2024/01/test-image.jpg",
            FileSize = 1024
        };

        var extractedMetadata = new Dictionary<string, string>
        {
            ["Width"] = "1920",
            ["Height"] = "1080"
        };

        _mockStorageRepository
            .Setup(x => x.UploadAsync(fileName, It.IsAny<Stream>(), mimeType, folderPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageResult);

        _mockMetadataExtractor
            .Setup(x => x.ExtractMetadataAsync(It.IsAny<Stream>(), mimeType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedMetadata);

        _mockMetadataRepository
            .Setup(x => x.AddAsync(It.IsAny<MediaItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMetadataRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UploadAsync(fileName, fileStream, mimeType, folderPath, uploadedBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fileName, result.FileName);
        Assert.Equal(".jpg", result.FileExtension);
        Assert.Equal(mimeType, result.MimeType);
        Assert.Equal(folderPath, result.FolderPath);
        Assert.Equal(storageResult.StoragePath, result.StoragePath);
        Assert.Equal(storageResult.PublicUrl, result.PublicUrl);
        Assert.Equal(storageResult.FileSize, result.FileSize);
        Assert.Equal(uploadedBy, result.UploadedBy);
        Assert.Equal(1920, result.Width);
        Assert.Equal(1080, result.Height);
        Assert.Equal(MediaStatus.Available, result.Status);
        Assert.NotNull(result.Id);
        Assert.NotNull(result.PartitionKey);

        _mockStorageRepository.Verify(x => x.UploadAsync(
            fileName, It.IsAny<Stream>(), mimeType, folderPath, It.IsAny<CancellationToken>()), Times.Once);
        _mockMetadataExtractor.Verify(x => x.ExtractMetadataAsync(
            It.IsAny<Stream>(), mimeType, It.IsAny<CancellationToken>()), Times.Once);
        _mockMetadataRepository.Verify(x => x.AddAsync(
            It.IsAny<MediaItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockMetadataRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_WithCustomMetadata_UsesProvidedValues()
    {
        // Arrange
        var fileName = "test.jpg";
        var mimeType = "image/jpeg";
        var folderPath = "/";
        var customTitle = "Custom Title";
        var customDescription = "Custom Description";
        var customAltText = "Custom Alt Text";
        using var fileStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var storageResult = new MediaStorageResult
        {
            StoragePath = "2024/01/test.jpg",
            PublicUrl = "https://cdn.example.com/test.jpg",
            FileSize = 512
        };

        _mockStorageRepository
            .Setup(x => x.UploadAsync(fileName, It.IsAny<Stream>(), mimeType, folderPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageResult);

        _mockMetadataExtractor
            .Setup(x => x.ExtractMetadataAsync(It.IsAny<Stream>(), mimeType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        MediaItem? capturedItem = null;
        _mockMetadataRepository
            .Setup(x => x.AddAsync(It.IsAny<MediaItem>(), It.IsAny<CancellationToken>()))
            .Callback<MediaItem, CancellationToken>((item, _) => capturedItem = item)
            .Returns(Task.CompletedTask);

        _mockMetadataRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UploadAsync(
            fileName, fileStream, mimeType, folderPath,
            customTitle, customDescription, customAltText);

        // Assert
        Assert.NotNull(capturedItem);
        Assert.Equal(customTitle, capturedItem.Title);
        Assert.Equal(customDescription, capturedItem.Description);
        Assert.Equal(customAltText, capturedItem.AltText);
    }

    [Fact]
    public async Task UploadAsync_WithNullFileName_ThrowsArgumentNullException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.UploadAsync(null!, stream, "image/jpeg", "/"));
    }

    [Fact]
    public async Task UploadAsync_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.UploadAsync("test.jpg", null!, "image/jpeg", "/"));
    }

    [Fact]
    public async Task UploadAsync_WithNullMimeType_ThrowsArgumentNullException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.UploadAsync("test.jpg", stream, null!, "/"));
    }

    [Fact]
    public async Task UploadAsync_WithStorageFailure_PropagatesException()
    {
        // Arrange
        var fileName = "test.jpg";
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        _mockStorageRepository
            .Setup(x => x.UploadAsync(fileName, It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Storage error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UploadAsync(fileName, stream, "image/jpeg", "/"));
        
        Assert.Equal("Storage error", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_SetsCorrectPartitionKey()
    {
        // Arrange
        var fileName = "test.jpg";
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        var now = DateTime.UtcNow;
        var expectedPartitionKey = now.ToString("yyyy-MM");

        var storageResult = new MediaStorageResult
        {
            StoragePath = "2024/01/test.jpg",
            PublicUrl = "https://cdn.example.com/test.jpg",
            FileSize = 512
        };

        _mockStorageRepository
            .Setup(x => x.UploadAsync(fileName, It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageResult);

        _mockMetadataExtractor
            .Setup(x => x.ExtractMetadataAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        MediaItem? capturedItem = null;
        _mockMetadataRepository
            .Setup(x => x.AddAsync(It.IsAny<MediaItem>(), It.IsAny<CancellationToken>()))
            .Callback<MediaItem, CancellationToken>((item, _) => capturedItem = item)
            .Returns(Task.CompletedTask);

        _mockMetadataRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UploadAsync(fileName, stream, "image/jpeg", "/");

        // Assert
        Assert.NotNull(capturedItem);
        Assert.Equal(expectedPartitionKey, capturedItem.PartitionKey);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/images")]
    [InlineData("/documents/work")]
    [InlineData("/media/2024/january")]
    public async Task UploadAsync_WithVariousFolderPaths_SetsCorrectly(string folderPath)
    {
        // Arrange
        var fileName = "test.jpg";
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var storageResult = new MediaStorageResult
        {
            StoragePath = "2024/01/test.jpg",
            PublicUrl = "https://cdn.example.com/test.jpg",
            FileSize = 512
        };

        _mockStorageRepository
            .Setup(x => x.UploadAsync(fileName, It.IsAny<Stream>(), It.IsAny<string>(), folderPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageResult);

        _mockMetadataExtractor
            .Setup(x => x.ExtractMetadataAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        MediaItem? capturedItem = null;
        _mockMetadataRepository
            .Setup(x => x.AddAsync(It.IsAny<MediaItem>(), It.IsAny<CancellationToken>()))
            .Callback<MediaItem, CancellationToken>((item, _) => capturedItem = item)
            .Returns(Task.CompletedTask);

        _mockMetadataRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UploadAsync(fileName, stream, "image/jpeg", folderPath);

        // Assert
        Assert.NotNull(capturedItem);
        Assert.Equal(folderPath, capturedItem.FolderPath);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsMediaItem()
    {
        // Arrange
        var id = "test-id";
        var partitionKey = "2024-01";
        var expectedItem = new MediaItem
        {
            Id = id,
            PartitionKey = partitionKey,
            FileName = "test.jpg",
            MimeType = "image/jpeg"
        };

        _mockMetadataRepository
            .Setup(x => x.GetByIdAsync(id, partitionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItem);

        // Act
        var result = await _service.GetByIdAsync(id, partitionKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("test.jpg", result.FileName);

        _mockMetadataRepository.Verify(x => x.GetByIdAsync(id, partitionKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var id = "non-existent-id";
        var partitionKey = "2024-01";

        _mockMetadataRepository
            .Setup(x => x.GetByIdAsync(id, partitionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItem?)null);

        // Act
        var result = await _service.GetByIdAsync(id, partitionKey);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithNullId_ReturnsNull()
    {
        // Arrange - The service doesn't throw on null, it passes to repository
        // which likely returns null
        _mockMetadataRepository
            .Setup(x => x.GetByIdAsync(null!, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItem?)null);

        // Act
        var result = await _service.GetByIdAsync(null!, "2024-01");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithNullPartitionKey_ReturnsNull()
    {
        // Arrange - The service doesn't throw on null, it passes to repository
        // which likely returns null
        _mockMetadataRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<string>(), null!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItem?)null);

        // Act
        var result = await _service.GetByIdAsync("test-id", null!);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_SoftDeletesItem()
    {
        // Arrange
        var id = "test-id";
        var partitionKey = "2024-01";
        var item = new MediaItem
        {
            Id = id,
            PartitionKey = partitionKey,
            FileName = "test.jpg",
            Status = MediaStatus.Available
        };

        _mockMetadataRepository
            .Setup(x => x.GetByIdAsync(id, partitionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        _mockMetadataRepository
            .Setup(x => x.UpdateAsync(It.IsAny<MediaItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMetadataRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(id, partitionKey);

        // Assert
        Assert.True(result);
        Assert.Equal(MediaStatus.Deleted, item.Status);
        Assert.NotNull(item.UpdatedAt);

        _mockMetadataRepository.Verify(x => x.UpdateAsync(
            It.Is<MediaItem>(m => m.Status == MediaStatus.Deleted),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify storage file is NOT deleted (soft delete)
        _mockStorageRepository.Verify(x => x.DeleteAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
    {
        // Arrange
        var id = "non-existent";
        var partitionKey = "2024-01";

        _mockMetadataRepository
            .Setup(x => x.GetByIdAsync(id, partitionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItem?)null);

        // Act
        var result = await _service.DeleteAsync(id, partitionKey);

        // Assert
        Assert.False(result);
        _mockMetadataRepository.Verify(x => x.UpdateAsync(
            It.IsAny<MediaItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithNullId_ReturnsFalse()
    {
        // Arrange - The service tries to get the item, repository returns null
        _mockMetadataRepository
            .Setup(x => x.GetByIdAsync(null!, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItem?)null);

        // Act
        var result = await _service.DeleteAsync(null!, "2024-01");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region UpdateMetadataAsync Tests

    [Fact]
    public async Task UpdateMetadataAsync_WithValidChanges_UpdatesItem()
    {
        // Arrange
        var id = "test-id";
        var partitionKey = "2024-01";
        var newTitle = "New Title";
        var newDescription = "New Description";
        var newAltText = "New Alt";

        var existingItem = new MediaItem
        {
            Id = id,
            PartitionKey = partitionKey,
            FileName = "test.jpg",
            Title = "Old Title",
            Description = "Old Description",
            AltText = "Old Alt"
        };

        _mockMetadataRepository
            .Setup(x => x.GetByIdAsync(id, partitionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingItem);

        _mockMetadataRepository
            .Setup(x => x.UpdateAsync(It.IsAny<MediaItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMetadataRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UpdateMetadataAsync(id, partitionKey, newTitle, newDescription, newAltText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newTitle, result.Title);
        Assert.Equal(newDescription, result.Description);
        Assert.Equal(newAltText, result.AltText);
        Assert.NotNull(result.UpdatedAt);

        _mockMetadataRepository.Verify(x => x.UpdateAsync(
            It.Is<MediaItem>(m =>
                m.Title == newTitle &&
                m.Description == newDescription &&
                m.AltText == newAltText),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateMetadataAsync_WithNonExistentItem_ReturnsNull()
    {
        // Arrange
        var id = "non-existent";
        var partitionKey = "2024-01";

        _mockMetadataRepository
            .Setup(x => x.GetByIdAsync(id, partitionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItem?)null);

        // Act
        var result = await _service.UpdateMetadataAsync(id, partitionKey, "Title", "Desc", "Alt");

        // Assert
        Assert.Null(result);
        _mockMetadataRepository.Verify(x => x.UpdateAsync(
            It.IsAny<MediaItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMetadataAsync_WithNullValues_UpdatesToNull()
    {
        // Arrange
        var id = "test-id";
        var partitionKey = "2024-01";

        var existingItem = new MediaItem
        {
            Id = id,
            PartitionKey = partitionKey,
            FileName = "test.jpg",
            Title = "Existing Title",
            Description = "Existing Description",
            AltText = "Existing Alt"
        };

        _mockMetadataRepository
            .Setup(x => x.GetByIdAsync(id, partitionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingItem);

        _mockMetadataRepository
            .Setup(x => x.UpdateAsync(It.IsAny<MediaItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMetadataRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UpdateMetadataAsync(id, partitionKey, null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Title);
        Assert.Null(result.Description);
        Assert.Null(result.AltText);
    }

    #endregion

    #region GetPublicUrlAsync Tests

    [Fact]
    public async Task GetPublicUrlAsync_WithValidItem_ReturnsUrl()
    {
        // Arrange
        var item = new MediaItem
        {
            Id = "test-id",
            StoragePath = "2024/01/test.jpg",
            PublicUrl = "https://cdn.example.com/test.jpg"
        };

        var expectedUrl = "https://cdn.example.com/test.jpg?token=abc123";

        _mockStorageRepository
            .Setup(x => x.GetPublicUrlAsync(item.StoragePath, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _service.GetPublicUrlAsync(item);

        // Assert
        Assert.Equal(expectedUrl, result);
        _mockStorageRepository.Verify(x => x.GetPublicUrlAsync(
            item.StoragePath, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPublicUrlAsync_WithExpiration_PassesExpirationToRepository()
    {
        // Arrange
        var item = new MediaItem
        {
            Id = "test-id",
            StoragePath = "2024/01/test.jpg"
        };
        var expiration = TimeSpan.FromHours(1);
        var expectedUrl = "https://cdn.example.com/test.jpg?sas=token&expires=...";

        _mockStorageRepository
            .Setup(x => x.GetPublicUrlAsync(item.StoragePath, expiration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _service.GetPublicUrlAsync(item, expiration);

        // Assert
        Assert.Equal(expectedUrl, result);
        _mockStorageRepository.Verify(x => x.GetPublicUrlAsync(
            item.StoragePath, expiration, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPublicUrlAsync_WithNullItem_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GetPublicUrlAsync(null!));
    }

    #endregion
}
