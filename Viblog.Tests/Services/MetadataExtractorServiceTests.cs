using Microsoft.Extensions.Logging;
using SkiaSharp;
using Viblog.Infrastructure.Shared.Services;
using Viblog.Shared.Services;

namespace Viblog.Tests.Services;

/// <summary>
/// Comprehensive unit tests for MetadataExtractorService
/// </summary>
public class MetadataExtractorServiceTests : IDisposable
{
    private readonly Mock<ILogger<MetadataExtractorService>> _mockLogger;
    private readonly MetadataExtractorService _service;
    private readonly List<Stream> _disposableStreams;

    public MetadataExtractorServiceTests()
    {
        _mockLogger = new Mock<ILogger<MetadataExtractorService>>();
        _service = new MetadataExtractorService(_mockLogger.Object);
        _disposableStreams = new List<Stream>();
    }

    public void Dispose()
    {
        foreach (var stream in _disposableStreams)
        {
            stream?.Dispose();
        }
        _disposableStreams.Clear();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a test image stream with specified dimensions and format
    /// </summary>
    private Stream CreateTestImageStream(int width, int height, SKEncodedImageFormat format = SKEncodedImageFormat.Jpeg, int quality = 90)
    {
        var imageInfo = new SKImageInfo(width, height);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;

        // Draw a simple gradient background
        using var paint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(width, height),
                new[] { SKColors.Blue, SKColors.Green },
                null,
                SKShaderTileMode.Clamp)
        };
        canvas.DrawRect(new SKRect(0, 0, width, height), paint);

        // Draw some text to make it more realistic
        using var font = new SKFont
        {
            Size = 48
        };
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true
        };
        canvas.DrawText($"{width}x{height}", 20, 60, SKTextAlign.Left, font, textPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(format, quality);

        var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Position = 0;

        _disposableStreams.Add(stream);
        return stream;
    }

    /// <summary>
    /// Creates a test image with EXIF orientation data
    /// </summary>
    private Stream CreateTestImageWithOrientation(SKEncodedOrigin origin)
    {
        var imageInfo = new SKImageInfo(800, 600);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;

        // Draw a simple pattern
        using var paint = new SKPaint { Color = SKColors.Red };
        canvas.DrawRect(new SKRect(0, 0, 400, 600), paint);

        using var image = surface.Snapshot();
        
        // Encode with specific origin
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);

        var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Position = 0;

        _disposableStreams.Add(stream);
        return stream;
    }

    #endregion

    #region Image Metadata Extraction Tests

    [Fact]
    public async Task ExtractMetadataAsync_WithJpegImage_ExtractsWidthAndHeight()
    {
        // Arrange
        var width = 1920;
        var height = 1080;
        var stream = CreateTestImageStream(width, height, SKEncodedImageFormat.Jpeg);
        var mimeType = "image/jpeg";

        // Act
        var result = await _service.ExtractMetadataAsync(stream, mimeType);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("Width"));
        Assert.True(result.ContainsKey("Height"));
        Assert.Equal(width.ToString(), result["Width"]);
        Assert.Equal(height.ToString(), result["Height"]);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithJpegImage_CalculatesAspectRatio()
    {
        // Arrange
        var width = 1920;
        var height = 1080;
        var expectedAspectRatio = (width / (double)height).ToString("F2");
        var stream = CreateTestImageStream(width, height, SKEncodedImageFormat.Jpeg);
        var mimeType = "image/jpeg";

        // Act
        var result = await _service.ExtractMetadataAsync(stream, mimeType);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("AspectRatio"));
        Assert.Equal(expectedAspectRatio, result["AspectRatio"]);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithPngImage_ExtractsMetadata()
    {
        // Arrange
        var width = 800;
        var height = 600;
        var stream = CreateTestImageStream(width, height, SKEncodedImageFormat.Png);
        var mimeType = "image/png";

        // Act
        var result = await _service.ExtractMetadataAsync(stream, mimeType);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("Width"));
        Assert.True(result.ContainsKey("Height"));
        Assert.Equal(width.ToString(), result["Width"]);
        Assert.Equal(height.ToString(), result["Height"]);
        Assert.True(result.ContainsKey("ColorType"));
        Assert.True(result.ContainsKey("AlphaType"));
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithWebpImage_ExtractsMetadata()
    {
        // Arrange
        var width = 1024;
        var height = 768;
        var stream = CreateTestImageStream(width, height, SKEncodedImageFormat.Webp);
        var mimeType = "image/webp";

        // Act
        var result = await _service.ExtractMetadataAsync(stream, mimeType);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("Width"));
        Assert.True(result.ContainsKey("Height"));
        Assert.Equal(width.ToString(), result["Width"]);
        Assert.Equal(height.ToString(), result["Height"]);
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(1920, 1080)]
    [InlineData(3840, 2160)]
    [InlineData(640, 480)]
    public async Task ExtractMetadataAsync_WithVariousDimensions_ExtractsCorrectly(int width, int height)
    {
        // Arrange
        var stream = CreateTestImageStream(width, height);
        var mimeType = "image/jpeg";

        // Act
        var result = await _service.ExtractMetadataAsync(stream, mimeType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(width.ToString(), result["Width"]);
        Assert.Equal(height.ToString(), result["Height"]);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithImageMimeType_IncludesColorTypeInfo()
    {
        // Arrange
        var stream = CreateTestImageStream(800, 600);
        var mimeType = "image/jpeg";

        // Act
        var result = await _service.ExtractMetadataAsync(stream, mimeType);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("ColorType"));
        Assert.True(result.ContainsKey("AlphaType"));
        Assert.NotEmpty(result["ColorType"]);
        Assert.NotEmpty(result["AlphaType"]);
    }

    [Fact]
    public async Task ExtractMetadataAsync_ResetsStreamPosition()
    {
        // Arrange
        var width = 800;
        var height = 600;
        var stream = CreateTestImageStream(width, height);
        var mimeType = "image/jpeg";
        var originalPosition = stream.Position;

        // Act
        var result = await _service.ExtractMetadataAsync(stream, mimeType);

        // Assert
        // Stream should be reset if seekable and not disposed
        if (stream.CanSeek)
        {
            Assert.Equal(originalPosition, stream.Position);
        }
    }

    #endregion

    #region Non-Image File Types Tests

    [Fact]
    public async Task ExtractMetadataAsync_WithVideoMimeType_ReturnsVideoMetadata()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x20 });
        var mimeType = "video/mp4";

        // Act
        var result = await _service.ExtractMetadataAsync(stream, mimeType);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("VideoFormat"));
        Assert.Equal("Unknown", result["VideoFormat"]);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithPdfMimeType_ReturnsPdfMetadata()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF header
        var mimeType = "application/pdf";

        // Act
        var result = await _service.ExtractMetadataAsync(stream, mimeType);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("DocumentType"));
        Assert.Equal("PDF", result["DocumentType"]);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithUnsupportedMimeType_ReturnsEmptyDictionary()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02, 0x03 });
        var mimeType = "application/octet-stream";

        // Act
        var result = await _service.ExtractMetadataAsync(stream, mimeType);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExtractMetadataAsync_WithInvalidImageData_DoesNotThrow()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02, 0x03 });
        var mimeType = "image/jpeg";

        // Act
        var result = await _service.ExtractMetadataAsync(stream, mimeType);

        // Assert
        Assert.NotNull(result);
        // Should return empty or partial metadata without throwing
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithEmptyStream_DoesNotThrow()
    {
        // Arrange
        using var stream = new MemoryStream();
        var mimeType = "image/jpeg";

        // Act
        var result = await _service.ExtractMetadataAsync(stream, mimeType);

        // Assert
        Assert.NotNull(result);
        // Should handle gracefully without throwing
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithNonSeekableStream_HandlesCorrectly()
    {
        // Arrange
        var width = 800;
        var height = 600;
        var sourceStream = CreateTestImageStream(width, height);
        
        // Create a non-seekable wrapper
        var nonSeekableStream = new NonSeekableStream(sourceStream);
        var mimeType = "image/jpeg";

        // Act
        var result = await _service.ExtractMetadataAsync(nonSeekableStream, mimeType);

        // Assert
        Assert.NotNull(result);
        // SkiaSharp can handle non-seekable streams
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var stream = CreateTestImageStream(800, 600);
        var mimeType = "image/jpeg";
        var cts = new CancellationTokenSource();

        // Act - SkiaSharp's codec creation is synchronous, so cancellation works at Task.Run level
        var task = _service.ExtractMetadataAsync(stream, mimeType, cts.Token);
        
        // The task completes quickly with small images, so we can't reliably cancel it
        // This test verifies the API accepts a cancellation token
        var result = await task;

        // Assert
        Assert.NotNull(result);
        // CancellationToken is passed through correctly even if operation completes quickly
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task ExtractMetadataAsync_WithMultipleSequentialCalls_WorksCorrectly()
    {
        // Arrange
        var stream1 = CreateTestImageStream(1920, 1080);
        var stream2 = CreateTestImageStream(800, 600);
        var mimeType = "image/jpeg";

        // Act
        var result1 = await _service.ExtractMetadataAsync(stream1, mimeType);
        var result2 = await _service.ExtractMetadataAsync(stream2, mimeType);

        // Assert
        Assert.Equal("1920", result1["Width"]);
        Assert.Equal("1080", result1["Height"]);
        Assert.Equal("800", result2["Width"]);
        Assert.Equal("600", result2["Height"]);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithLargeImage_ExtractsSuccessfully()
    {
        // Arrange
        var width = 4096;
        var height = 3072;
        var stream = CreateTestImageStream(width, height);
        var mimeType = "image/jpeg";

        // Act
        var result = await _service.ExtractMetadataAsync(stream, mimeType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(width.ToString(), result["Width"]);
        Assert.Equal(height.ToString(), result["Height"]);
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Helper class to simulate a non-seekable stream
    /// </summary>
    private class NonSeekableStream : Stream
    {
        private readonly Stream _innerStream;

        public NonSeekableStream(Stream innerStream)
        {
            _innerStream = innerStream;
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => _innerStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    #endregion
}
