using Viblog.Shared.Helpers;
using Xunit;

namespace Viblog.Tests.Helpers;

/// <summary>
/// Unit tests for MediaIconHelper
/// </summary>
public class MediaIconHelperTests
{
    #region Image Files Tests

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    [InlineData("image/svg+xml")]
    public void GetFileTypeIcon_WithImageMimeType_ReturnsNull(string mimeType)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon(mimeType);

        // Assert - Images should display actual image, not icon
        Assert.Null(result);
    }

    #endregion

    #region PDF Tests

    [Fact]
    public void GetFileTypeIcon_WithPdfMimeType_ReturnsPdfIcon()
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("application/pdf");

        // Assert
        Assert.Equal("/icons/file-pdf.svg", result);
    }

    [Fact]
    public void GetFileTypeIcon_WithPdfExtension_ReturnsPdfIcon()
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", "document.pdf");

        // Assert
        Assert.Equal("/icons/file-pdf.svg", result);
    }

    #endregion

    #region Video Tests

    [Theory]
    [InlineData("video/mp4")]
    [InlineData("video/webm")]
    [InlineData("video/ogg")]
    [InlineData("video/quicktime")]
    public void GetFileTypeIcon_WithVideoMimeType_ReturnsVideoIcon(string mimeType)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon(mimeType);

        // Assert
        Assert.Equal("/icons/file-video.svg", result);
    }

    [Theory]
    [InlineData("video.mp4")]
    [InlineData("video.avi")]
    [InlineData("video.mov")]
    [InlineData("video.wmv")]
    public void GetFileTypeIcon_WithVideoExtension_ReturnsVideoIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("application/octet-stream", fileName);

        // Assert
        Assert.Equal("/icons/file-video.svg", result);
    }

    #endregion

    #region Audio Tests

    [Theory]
    [InlineData("audio/mpeg")]
    [InlineData("audio/wav")]
    [InlineData("audio/ogg")]
    [InlineData("audio/webm")]
    public void GetFileTypeIcon_WithAudioMimeType_ReturnsAudioIcon(string mimeType)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon(mimeType);

        // Assert
        Assert.Equal("/icons/file-audio.svg", result);
    }

    [Theory]
    [InlineData("song.mp3")]
    [InlineData("song.wav")]
    [InlineData("song.ogg")]
    [InlineData("song.m4a")]
    [InlineData("song.flac")]
    public void GetFileTypeIcon_WithAudioExtension_ReturnsAudioIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("application/octet-stream", fileName);

        // Assert
        Assert.Equal("/icons/file-audio.svg", result);
    }

    #endregion

    #region Office Documents Tests

    [Theory]
    [InlineData("application/msword")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void GetFileTypeIcon_WithWordMimeType_ReturnsDocumentIcon(string mimeType)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon(mimeType);

        // Assert
        Assert.Equal("/icons/file-document.svg", result);
    }

    [Theory]
    [InlineData("document.doc")]
    [InlineData("document.docx")]
    [InlineData("document.rtf")]
    public void GetFileTypeIcon_WithWordExtension_ReturnsDocumentIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("application/octet-stream", fileName);

        // Assert
        Assert.Equal("/icons/file-document.svg", result);
    }

    [Theory]
    [InlineData("application/vnd.ms-excel")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public void GetFileTypeIcon_WithExcelMimeType_ReturnsSpreadsheetIcon(string mimeType)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon(mimeType);

        // Assert
        Assert.Equal("/icons/file-spreadsheet.svg", result);
    }

    [Theory]
    [InlineData("spreadsheet.xls")]
    [InlineData("spreadsheet.xlsx")]
    [InlineData("data.csv")]
    public void GetFileTypeIcon_WithExcelExtension_ReturnsSpreadsheetIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("application/octet-stream", fileName);

        // Assert
        Assert.Equal("/icons/file-spreadsheet.svg", result);
    }

    [Theory]
    [InlineData("application/vnd.ms-powerpoint")]
    [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    public void GetFileTypeIcon_WithPowerPointMimeType_ReturnsPresentationIcon(string mimeType)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon(mimeType);

        // Assert
        Assert.Equal("/icons/file-presentation.svg", result);
    }

    [Theory]
    [InlineData("presentation.ppt")]
    [InlineData("presentation.pptx")]
    [InlineData("presentation.pps")]
    [InlineData("presentation.ppsx")]
    public void GetFileTypeIcon_WithPowerPointExtension_ReturnsPresentationIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("application/octet-stream", fileName);

        // Assert
        Assert.Equal("/icons/file-presentation.svg", result);
    }

    #endregion

    #region Archive Tests

    [Theory]
    [InlineData("application/zip")]
    [InlineData("application/x-rar-compressed")]
    [InlineData("application/x-7z-compressed")]
    [InlineData("application/gzip")]
    public void GetFileTypeIcon_WithArchiveMimeType_ReturnsArchiveIcon(string mimeType)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon(mimeType);

        // Assert
        Assert.Equal("/icons/file-archive.svg", result);
    }

    [Theory]
    [InlineData("archive.zip")]
    [InlineData("archive.rar")]
    [InlineData("archive.7z")]
    [InlineData("archive.tar")]
    [InlineData("archive.gz")]
    public void GetFileTypeIcon_WithArchiveExtension_ReturnsArchiveIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("application/octet-stream", fileName);

        // Assert
        Assert.Equal("/icons/file-archive.svg", result);
    }

    #endregion

    #region Code Files Tests

    [Theory]
    [InlineData("Program.cs")]
    [InlineData("Module.vb")]
    [InlineData("Script.fs")]
    public void GetFileTypeIcon_WithDotNetCodeExtension_ReturnsCodeIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", fileName);

        // Assert
        Assert.Equal("/icons/file-code.svg", result);
    }

    [Theory]
    [InlineData("app.js")]
    [InlineData("component.ts")]
    [InlineData("Component.jsx")]
    [InlineData("Component.tsx")]
    public void GetFileTypeIcon_WithJavaScriptTypeScriptExtension_ReturnsCodeIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", fileName);

        // Assert
        Assert.Equal("/icons/file-code.svg", result);
    }

    [Theory]
    [InlineData("index.html")]
    [InlineData("page.htm")]
    [InlineData("data.xml")]
    [InlineData("MainWindow.xaml")]
    public void GetFileTypeIcon_WithMarkupExtension_ReturnsCodeIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", fileName);

        // Assert
        Assert.Equal("/icons/file-code.svg", result);
    }

    [Theory]
    [InlineData("styles.css")]
    [InlineData("theme.scss")]
    [InlineData("variables.sass")]
    [InlineData("mixins.less")]
    public void GetFileTypeIcon_WithStylesheetExtension_ReturnsCodeIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", fileName);

        // Assert
        Assert.Equal("/icons/file-code.svg", result);
    }

    [Theory]
    [InlineData("config.json")]
    [InlineData("config.yaml")]
    [InlineData("config.yml")]
    public void GetFileTypeIcon_WithDataFormatExtension_ReturnsCodeIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", fileName);

        // Assert
        Assert.Equal("/icons/file-code.svg", result);
    }

    [Theory]
    [InlineData("Main.java")]
    [InlineData("App.kt")]
    [InlineData("Service.scala")]
    public void GetFileTypeIcon_WithJvmLanguageExtension_ReturnsCodeIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", fileName);

        // Assert
        Assert.Equal("/icons/file-code.svg", result);
    }

    [Theory]
    [InlineData("script.py")]
    [InlineData("app.rb")]
    [InlineData("index.php")]
    public void GetFileTypeIcon_WithScriptingExtension_ReturnsCodeIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", fileName);

        // Assert
        Assert.Equal("/icons/file-code.svg", result);
    }

    [Theory]
    [InlineData("main.cpp")]
    [InlineData("main.c")]
    [InlineData("header.h")]
    [InlineData("header.hpp")]
    public void GetFileTypeIcon_WithCppExtension_ReturnsCodeIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", fileName);

        // Assert
        Assert.Equal("/icons/file-code.svg", result);
    }

    [Theory]
    [InlineData("main.go")]
    [InlineData("main.rs")]
    [InlineData("ViewController.swift")]
    public void GetFileTypeIcon_WithModernLanguageExtension_ReturnsCodeIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", fileName);

        // Assert
        Assert.Equal("/icons/file-code.svg", result);
    }

    [Theory]
    [InlineData("query.sql")]
    [InlineData("script.sh")]
    [InlineData("script.bat")]
    [InlineData("script.ps1")]
    public void GetFileTypeIcon_WithShellSqlExtension_ReturnsCodeIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", fileName);

        // Assert
        Assert.Equal("/icons/file-code.svg", result);
    }

    #endregion

    #region Text Files Tests

    [Theory]
    [InlineData("readme.txt")]
    [InlineData("README.md")]
    [InlineData("error.log")]
    public void GetFileTypeIcon_WithTextExtension_ReturnsTextIcon(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", fileName);

        // Assert
        Assert.Equal("/icons/file-text.svg", result);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void GetFileTypeIcon_WithNullMimeType_UsesExtensionFallback()
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon(null!, "document.pdf");

        // Assert
        Assert.Equal("/icons/file-pdf.svg", result);
    }

    [Fact]
    public void GetFileTypeIcon_WithEmptyMimeType_UsesExtensionFallback()
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("", "document.pdf");

        // Assert
        Assert.Equal("/icons/file-pdf.svg", result);
    }

    [Fact]
    public void GetFileTypeIcon_WithUnknownMimeTypeAndNoExtension_ReturnsUnknownIcon()
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("application/unknown");

        // Assert
        Assert.Equal("/icons/file-unknown.svg", result);
    }

    [Fact]
    public void GetFileTypeIcon_WithUnknownExtension_ReturnsUnknownIcon()
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("application/octet-stream", "file.xyz");

        // Assert
        Assert.Equal("/icons/file-unknown.svg", result);
    }

    [Fact]
    public void GetFileTypeIcon_WithNullFileName_UsesOnlyMimeType()
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("application/pdf", null);

        // Assert
        Assert.Equal("/icons/file-pdf.svg", result);
    }

    [Fact]
    public void GetFileTypeIcon_WithEmptyFileName_UsesOnlyMimeType()
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("application/pdf", "");

        // Assert
        Assert.Equal("/icons/file-pdf.svg", result);
    }

    #endregion

    #region Case Insensitivity Tests

    [Theory]
    [InlineData("APPLICATION/PDF")]
    [InlineData("Application/Pdf")]
    [InlineData("application/PDF")]
    public void GetFileTypeIcon_WithMixedCaseMimeType_IsCaseInsensitive(string mimeType)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon(mimeType);

        // Assert
        Assert.Equal("/icons/file-pdf.svg", result);
    }

    [Theory]
    [InlineData("FILE.PDF")]
    [InlineData("File.Pdf")]
    [InlineData("file.PDF")]
    public void GetFileTypeIcon_WithMixedCaseExtension_IsCaseInsensitive(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("application/octet-stream", fileName);

        // Assert
        Assert.Equal("/icons/file-pdf.svg", result);
    }

    [Theory]
    [InlineData("PROGRAM.CS")]
    [InlineData("Program.CS")]
    [InlineData("program.cs")]
    public void GetFileTypeIcon_WithMixedCaseCodeExtension_IsCaseInsensitive(string fileName)
    {
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", fileName);

        // Assert
        Assert.Equal("/icons/file-code.svg", result);
    }

    #endregion

    #region Fallback Priority Tests

    [Fact]
    public void GetFileTypeIcon_WithConflictingMimeTypeAndExtension_PrioritizesMimeType()
    {
        // MIME type says PDF, but extension says JPG
        // Should return PDF icon because MIME type is checked first
        
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("application/pdf", "file.jpg");

        // Assert
        Assert.Equal("/icons/file-pdf.svg", result);
    }

    [Fact]
    public void GetFileTypeIcon_WithGenericMimeTypeAndCodeExtension_UsesExtension()
    {
        // Generic MIME type, but .cs extension should return code icon
        
        // Act
        var result = MediaIconHelper.GetFileTypeIcon("text/plain", "Program.cs");

        // Assert
        Assert.Equal("/icons/file-code.svg", result);
    }

    #endregion
}
