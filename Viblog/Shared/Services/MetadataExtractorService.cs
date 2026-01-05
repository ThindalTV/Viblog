using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata;

namespace Viblog.Shared.Services;

/// <summary>
/// Service implementation for extracting metadata from various media file types
/// </summary>
public class MetadataExtractorService : IMetadataExtractorService
{
    private readonly ILogger<MetadataExtractorService> _logger;

    public MetadataExtractorService(ILogger<MetadataExtractorService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> ExtractMetadataAsync(
        Stream fileStream,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        var metadata = new Dictionary<string, string>();

        try
        {
            // Ensure stream is at the beginning
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                await ExtractImageMetadataAsync(fileStream, metadata, cancellationToken);
            }
            else if (mimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                ExtractVideoMetadata(fileStream, metadata);
            }
            else if (mimeType == "application/pdf")
            {
                ExtractPdfMetadata(fileStream, metadata);
            }

            // Reset stream position if possible
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract metadata for MIME type: {MimeType}", mimeType);
            // Don't throw - metadata extraction is optional
        }

        return metadata;
    }

    /// <summary>
    /// Extract metadata from image files using ImageSharp
    /// </summary>
    private async Task ExtractImageMetadataAsync(
        Stream fileStream,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            using var image = await Image.LoadAsync(fileStream, cancellationToken);

            metadata["Width"] = image.Width.ToString();
            metadata["Height"] = image.Height.ToString();
            metadata["AspectRatio"] = (image.Width / (double)image.Height).ToString("F2");

            // Extract EXIF data if available
            var exifProfile = image.Metadata.ExifProfile;
            if (exifProfile != null)
            {
                // Extract common EXIF tags
                if (exifProfile.TryGetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.Make, out var make))
                {
                    metadata["CameraMake"] = make.Value?.ToString() ?? string.Empty;
                }

                if (exifProfile.TryGetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.Model, out var model))
                {
                    metadata["CameraModel"] = model.Value?.ToString() ?? string.Empty;
                }

                if (exifProfile.TryGetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.DateTime, out var dateTime))
                {
                    metadata["DateTaken"] = dateTime.Value?.ToString() ?? string.Empty;
                }

                if (exifProfile.TryGetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.Orientation, out var orientation))
                {
                    metadata["Orientation"] = orientation.Value.ToString();
                }
            }

            _logger.LogInformation("Extracted image metadata: {Width}x{Height}", image.Width, image.Height);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract image metadata");
        }
    }

    /// <summary>
    /// Extract metadata from video files
    /// Note: Basic implementation - would need additional library like FFmpeg for full video metadata
    /// </summary>
    private void ExtractVideoMetadata(Stream fileStream, Dictionary<string, string> metadata)
    {
        try
        {
            // Placeholder for video metadata extraction
            // In production, you would use a library like FFMpegCore or MediaInfo
            metadata["VideoFormat"] = "Unknown";
            
            _logger.LogInformation("Video metadata extraction not fully implemented - placeholder data added");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract video metadata");
        }
    }

    /// <summary>
    /// Extract metadata from PDF files
    /// Note: Basic implementation - would need PDF library for full PDF metadata
    /// </summary>
    private void ExtractPdfMetadata(Stream fileStream, Dictionary<string, string> metadata)
    {
        try
        {
            // Placeholder for PDF metadata extraction
            // In production, you would use a library like PdfSharp or iTextSharp
            metadata["DocumentType"] = "PDF";
            
            _logger.LogInformation("PDF metadata extraction not fully implemented - placeholder data added");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract PDF metadata");
        }
    }
}
