using FFMpeg.MCP.Host.Mcp;
using FFMpeg.MCP.Host.Tools;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tests;

public class AudioMetadataToolsTests : TestBase
{
    private readonly AudioMetadataTools _metadataTools;

    public AudioMetadataToolsTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dispatcherLogger = loggerFactory.CreateLogger<McpDispatcher>();
        var dispatcher = new McpDispatcher(dispatcherLogger);
        _metadataTools = new AudioMetadataTools(FFmpegService, loggerFactory.CreateLogger<AudioMetadataTools>(), dispatcher);
    }

    [Fact]
    public async Task GetAudioFileInfoAsync_WithValidFile_ReturnsFileInfo()
    {
        // Arrange
        var testFile = CopyTestFile("sample-short.mp3");

        // Act
        var response = await _metadataTools.GetAudioFileInfoAsync(testFile);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        Assert.Equal("sample-short.mp3", response.Result.FileName);
    }

    [Fact]
    public async Task GetAudioFileInfoAsync_WithNonExistentFile_ReturnsErrorMessage()
    {
        // Arrange
        var nonExistentFile = GetWorkingPath("nonexistent.mp3");

        // Act
        var response = await _metadataTools.GetAudioFileInfoAsync(nonExistentFile);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal("not_found", response.Error.Code);
    }

    [Fact]
    public async Task UpdateCommonMetadataAsync_WithValidData_UpdatesMetadata()
    {
        // Arrange
        var testFile = CopyTestFile("sample-short.mp3");
        var outputFile = GetWorkingPath("updated-metadata.mp3");

        // Act
        var response = await _metadataTools.UpdateCommonMetadataAsync(
            testFile,
            title: "Test Title",
            outputPath: outputFile
        );

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        AssertFileExists(outputFile);
    }

    [Fact]
    public async Task UpdateAudioMetadataAsync_WithValidJson_UpdatesMetadata()
    {
        // Arrange
        var testFile = CopyTestFile("sample-short.mp3");
        var outputFile = GetWorkingPath("custom-metadata.mp3");
        var metadataJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["title"] = "Custom Title"
        });

        // Act
        var response = await _metadataTools.UpdateAudioMetadataAsync(testFile, metadataJson, outputFile);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        AssertFileExists(outputFile);
    }

    [Fact]
    public async Task UpdateAudioMetadataAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var testFile = CopyTestFile("sample-short.mp3");
        var invalidJson = "{ invalid json }";

        // Act
        var response = await _metadataTools.UpdateAudioMetadataAsync(testFile, invalidJson);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal("internal_error", response.Error.Code);
    }

    [Fact]
    public async Task GetSupportedFormatsAsync_ReturnsFormats()
    {
        // Act
        var response = await _metadataTools.GetSupportedFormatsAsync();

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        Assert.True(response.Result.Count > 0);
    }

    [Fact]
    public async Task CheckFFmpegAvailabilityAsync_ChecksAvailability()
    {
        // Act
        var response = await _metadataTools.CheckFFmpegAvailabilityAsync();

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        Assert.True(response.Result.FfmpegAvailable);
    }
}