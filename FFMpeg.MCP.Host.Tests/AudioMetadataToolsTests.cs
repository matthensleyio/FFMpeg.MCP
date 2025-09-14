using FFMpeg.MCP.Host.Tools;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tests;

public class AudioMetadataToolsTests : TestBase
{
    private readonly AudioMetadataTools _metadataTools;

    public AudioMetadataToolsTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AudioMetadataTools>();
        _metadataTools = new AudioMetadataTools(FFmpegService, logger);
    }

    [Fact]
    public async Task GetAudioFileInfoAsync_WithValidFile_ReturnsFileInfo()
    {
        // Arrange
        var testFile = CopyTestFile("sample-short.mp3");

        // Act
        var result = await _metadataTools.GetAudioFileInfoAsync(testFile);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result));

        var response = await DeserializeResponse<dynamic>(result);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetAudioFileInfoAsync_WithNonExistentFile_ReturnsErrorMessage()
    {
        // Arrange
        var nonExistentFile = GetWorkingPath("nonexistent.mp3");

        // Act
        var result = await _metadataTools.GetAudioFileInfoAsync(nonExistentFile);

        // Assert
        Assert.Contains("Could not analyze audio file", result);
    }

    [Fact]
    public async Task UpdateCommonMetadataAsync_WithValidData_UpdatesMetadata()
    {
        // Arrange
        var testFile = CopyTestFile("sample-short.mp3");
        var outputFile = GetWorkingPath("updated-metadata.mp3");

        // Act
        var result = await _metadataTools.UpdateCommonMetadataAsync(
            testFile,
            title: "Test Title",
            artist: "Test Artist",
            album: "Test Album",
            genre: "Test Genre",
            year: "2024",
            track: "1",
            comment: "Test Comment",
            outputPath: outputFile
        );

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean(), response.GetProperty("message").GetString());
        AssertFileExists(outputFile);
    }

    [Fact]
    public async Task UpdateCommonMetadataAsync_WithNoFieldsProvided_ReturnsError()
    {
        // Arrange
        var testFile = CopyTestFile("sample-short.mp3");

        // Act
        var result = await _metadataTools.UpdateCommonMetadataAsync(testFile);

        // Assert
        Assert.Contains("No metadata fields provided to update", result);
    }

    [Fact]
    public async Task UpdateAudioMetadataAsync_WithValidJson_UpdatesMetadata()
    {
        // Arrange
        var testFile = CopyTestFile("sample-short.mp3");
        var outputFile = GetWorkingPath("custom-metadata.mp3");
        var metadataJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["title"] = "Custom Title",
            ["artist"] = "Custom Artist",
            ["composer"] = "Custom Composer"
        });

        // Act
        var result = await _metadataTools.UpdateAudioMetadataAsync(testFile, metadataJson, outputFile);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("success").GetBoolean(), response.GetProperty("message").GetString());
        AssertFileExists(outputFile);
    }

    [Fact]
    public async Task UpdateAudioMetadataAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var testFile = CopyTestFile("sample-short.mp3");
        var invalidJson = "{ invalid json }";

        // Act
        var result = await _metadataTools.UpdateAudioMetadataAsync(testFile, invalidJson);

        // Assert
        Assert.Contains("Error updating metadata", result);
    }

    [Fact]
    public async Task GetSupportedFormatsAsync_ReturnsFormats()
    {
        // Act
        var result = await _metadataTools.GetSupportedFormatsAsync();

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.GetProperty("supportedFormats").GetArrayLength() > 0);
        Assert.True(response.GetProperty("count").GetInt32() > 0);
    }

    [Fact]
    public async Task CheckFFmpegAvailabilityAsync_ChecksAvailability()
    {
        // Act
        var result = await _metadataTools.CheckFFmpegAvailabilityAsync();

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(response.TryGetProperty("ffmpegAvailable", out _));
        Assert.True(response.TryGetProperty("message", out _));
    }
}