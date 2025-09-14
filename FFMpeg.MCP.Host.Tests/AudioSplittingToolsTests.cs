using FFMpeg.MCP.Host.Mcp;
using FFMpeg.MCP.Host.Tools;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tests;

public class AudioSplittingToolsTests : TestBase
{
    private readonly AudioSplittingTools _splittingTools;

    public AudioSplittingToolsTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dispatcherLogger = loggerFactory.CreateLogger<McpDispatcher>();
        var dispatcher = new McpDispatcher(dispatcherLogger);
        _splittingTools = new AudioSplittingTools(FFmpegService, loggerFactory.CreateLogger<AudioSplittingTools>(), dispatcher);
    }

    [Fact]
    public async Task SplitAudioByDurationAsync_WithValidFile_CreatesSplitFiles()
    {
        // Arrange
        var testFile = CopyTestFile("long-form.mp3");

        // Act
        var response = await _splittingTools.SplitAudioByDurationAsync(testFile, maxDurationSeconds: 60);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        Assert.True(response.Result.FilesCreated > 0);
    }

    [Fact]
    public async Task SplitAudioByMinutesAsync_WithValidFile_CreatesSplitFiles()
    {
        // Arrange
        var testFile = CopyTestFile("long-form.mp3");

        // Act
        var response = await _splittingTools.SplitAudioByMinutesAsync(testFile, maxDurationMinutes: 2.0);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        Assert.True(response.Result.FilesCreated > 0);
    }

    [Fact]
    public async Task SplitAudioByChaptersAsync_WithChapterFile_CreatesSplitFiles()
    {
        // Arrange
        var testFile = CopyTestFile("with-chapters.m4a");

        // Act
        var response = await _splittingTools.SplitAudioByChaptersAsync(testFile, preserveMetadata: true);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        Assert.True(response.Result.FilesCreated > 0);
    }

    [Fact]
    public async Task GetAudioChaptersAsync_WithValidFile_ReturnsChapterInfo()
    {
        // Arrange
        var testFile = CopyTestFile("with-chapters.m4a");

        // Act
        var response = await _splittingTools.GetAudioChaptersAsync(testFile);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        Assert.True(response.Result.HasChapters);
        Assert.True(response.Result.ChapterCount > 0);
    }

    [Fact]
    public async Task SplitAudioByDurationAsync_WithNonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentFile = GetWorkingPath("nonexistent.mp3");

        // Act
        var response = await _splittingTools.SplitAudioByDurationAsync(nonExistentFile, maxDurationSeconds: 60);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal("not_found", response.Error.Code);
    }
}