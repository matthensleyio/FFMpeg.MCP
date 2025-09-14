using FFMpeg.MCP.Host.Mcp;
using FFMpeg.MCP.Host.Tools;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tests;

public class AudioChapterToolsTests : TestBase
{
    private readonly AudioChapterTools _chapterTools;

    public AudioChapterToolsTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dispatcherLogger = loggerFactory.CreateLogger<McpDispatcher>();
        var dispatcher = new McpDispatcher(dispatcherLogger);
        _chapterTools = new AudioChapterTools(FFmpegService, loggerFactory.CreateLogger<AudioChapterTools>(), dispatcher);
    }

    [Fact]
    public async Task SetAudioChaptersAsync_WithValidChapters_SetsChapters()
    {
        // Arrange
        var inputFile = CopyTestFile("long-form.mp3");
        var outputFile = GetWorkingPath("with-chapters.mp3");
        var chapters = JsonSerializer.Serialize(new[]
        {
            new { startTime = "00:00:00", endTime = "00:02:00", title = "Introduction" },
            new { startTime = "00:02:00", endTime = "00:04:00", title = "Main Content" }
        });

        // Act
        var response = await _chapterTools.SetAudioChaptersAsync(inputFile, chapters, outputFile);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        AssertFileExists(outputFile);
        Assert.Equal(2, response.Result.ChaptersAdded);
        Assert.Equal("Introduction", response.Result.Chapters[0].Title);
    }

    [Fact]
    public async Task GenerateChaptersBySilenceAsync_ShowsNotImplementedMessage()
    {
        // Arrange
        var inputFile = CopyTestFile("long-speech.wav");

        // Act
        var response = await _chapterTools.GenerateChaptersBySilenceAsync(inputFile);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal("internal_error", response.Error.Code);
        Assert.Contains("not yet fully implemented", response.Error.Message);
    }

    [Fact]
    public async Task RemoveChaptersAsync_WithChapterFile_RemovesChapters()
    {
        // Arrange
        var inputFile = CopyTestFile("with-chapters.m4a");
        var outputFile = GetWorkingPath("no-chapters.m4a");

        // Act
        var response = await _chapterTools.RemoveChaptersAsync(inputFile, outputFile);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        AssertFileExists(outputFile);
        Assert.Equal("Chapters removed successfully", response.Result.Message);
    }

    [Fact]
    public async Task ExportChapterInfoAsync_ToJson_ExportsChapters()
    {
        // Arrange
        var inputFile = CopyTestFile("with-chapters.m4a");
        var outputFile = GetWorkingPath("chapters.json");

        // Act
        var response = await _chapterTools.ExportChapterInfoAsync(inputFile, format: "json", outputPath: outputFile);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        AssertFileExists(outputFile);
        Assert.Equal("JSON", response.Result.Format);
    }

    [Fact]
    public async Task SetAudioChaptersAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var inputFile = CopyTestFile("very-short.mp3");
        var invalidChapters = "{ invalid json }";

        // Act
        var response = await _chapterTools.SetAudioChaptersAsync(inputFile, invalidChapters);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal("internal_error", response.Error.Code);
    }

    [Fact]
    public async Task GenerateEqualChaptersAsync_WithNonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentFile = GetWorkingPath("nonexistent.mp3");

        // Act
        var response = await _chapterTools.GenerateEqualChaptersAsync(nonExistentFile, chapterDurationMinutes: 2.0);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal("not_found", response.Error.Code);
    }
}