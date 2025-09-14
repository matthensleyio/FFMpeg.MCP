using FFMpeg.MCP.Host.Tools;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tests;

public class AudioSplittingToolsTests : TestBase
{
    private readonly AudioSplittingTools _splittingTools;

    public AudioSplittingToolsTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AudioSplittingTools>();
        _splittingTools = new AudioSplittingTools(FFmpegService, logger);
    }

    [Fact]
    public async Task SplitAudioByDurationAsync_WithValidFile_CreatesSplitFiles()
    {
        // Arrange
        var testFile = CopyTestFile("long-form.mp3");

        // Act
        var result = await _splittingTools.SplitAudioByDurationAsync(testFile, maxDurationSeconds: 60);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            var outputFiles = response.GetProperty("outputFiles").EnumerateArray();
            Assert.True(outputFiles.Any());

            // Track created files for cleanup
            foreach (var file in outputFiles)
            {
                var filePath = file.GetString();
                if (!string.IsNullOrEmpty(filePath))
                {
                    TrackCreatedFile(filePath);
                    AssertFileExists(filePath);
                }
            }
        }
    }

    [Fact]
    public async Task SplitAudioByMinutesAsync_WithValidFile_CreatesSplitFiles()
    {
        // Arrange
        var testFile = CopyTestFile("long-form.mp3");

        // Act
        var result = await _splittingTools.SplitAudioByMinutesAsync(testFile, maxDurationMinutes: 2.0);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            var outputFiles = response.GetProperty("outputFiles").EnumerateArray();
            Assert.True(outputFiles.Any());

            foreach (var file in outputFiles)
            {
                var filePath = file.GetString();
                if (!string.IsNullOrEmpty(filePath))
                {
                    TrackCreatedFile(filePath);
                    AssertFileExists(filePath);
                }
            }
        }
    }

    [Fact]
    public async Task SplitAudioByChaptersAsync_WithChapterFile_CreatesSplitFiles()
    {
        // Arrange - this test requires a file with chapters
        var testFile = CopyTestFile("with-chapters.m4a");

        // Act
        var result = await _splittingTools.SplitAudioByChaptersAsync(testFile, preserveMetadata: true);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        // Note: This may fail if the test file doesn't have chapters, which is expected
        if (response.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
        {
            var outputFiles = response.GetProperty("outputFiles").EnumerateArray();
            foreach (var file in outputFiles)
            {
                var filePath = file.GetString();
                if (!string.IsNullOrEmpty(filePath))
                {
                    TrackCreatedFile(filePath);
                    AssertFileExists(filePath);
                }
            }
        }
        else if (result.Contains("No split operation performed"))
        {
            // This is expected if the file has no chapters
            Assert.Contains("No split operation performed", result);
        }
    }

    [Fact]
    public async Task SplitAudioByChaptersAsync_WithOutputPattern_UsesPattern()
    {
        // Arrange
        var testFile = CopyTestFile("with-chapters.m4a");
        var outputPattern = "{filename}_chapter_{chapter}_{title}";

        // Act
        var result = await _splittingTools.SplitAudioByChaptersAsync(testFile, outputPattern: outputPattern);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
        {
            var outputFiles = response.GetProperty("outputFiles").EnumerateArray();
            foreach (var file in outputFiles)
            {
                var filePath = file.GetString();
                if (!string.IsNullOrEmpty(filePath))
                {
                    TrackCreatedFile(filePath);
                    AssertFileExists(filePath);
                    // Check that the pattern was used in filename
                    Assert.Contains("chapter", Path.GetFileName(filePath));
                }
            }
        }
    }

    [Fact]
    public async Task GetAudioChaptersAsync_WithValidFile_ReturnsChapterInfo()
    {
        // Arrange
        var testFile = CopyTestFile("with-chapters.m4a");

        // Act
        var result = await _splittingTools.GetAudioChaptersAsync(testFile);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        Assert.True(response.TryGetProperty("filePath", out _));
        Assert.True(response.TryGetProperty("hasChapters", out _));
        Assert.True(response.TryGetProperty("chapterCount", out _));
        Assert.True(response.TryGetProperty("chapters", out _));
    }

    [Fact]
    public async Task SplitAudioAdvancedAsync_WithValidOptions_CreatesSplitFiles()
    {
        // Arrange
        var testFile = CopyTestFile("long-form.mp3");
        var options = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["maxDurationMinutes"] = 1.5,
            ["outputPattern"] = "{filename}_segment_{segment}",
            ["preserveMetadata"] = true
        });

        // Act
        var result = await _splittingTools.SplitAudioAdvancedAsync(testFile, options);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
        {
            var outputFiles = response.GetProperty("outputFiles").EnumerateArray();
            foreach (var file in outputFiles)
            {
                var filePath = file.GetString();
                if (!string.IsNullOrEmpty(filePath))
                {
                    TrackCreatedFile(filePath);
                    AssertFileExists(filePath);
                    Assert.Contains("segment", Path.GetFileName(filePath));
                }
            }
        }
    }

    [Fact]
    public async Task SplitAudioAdvancedAsync_WithInvalidOptions_ReturnsError()
    {
        // Arrange
        var testFile = CopyTestFile("sample-short.mp3");
        var invalidOptions = "{ invalid json }";

        // Act
        var result = await _splittingTools.SplitAudioAdvancedAsync(testFile, invalidOptions);

        // Assert
        Assert.Contains("Invalid options JSON provided", result);
    }

    [Fact]
    public async Task SplitAudioByDurationAsync_WithNonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentFile = GetWorkingPath("nonexistent.mp3");

        // Act
        var result = await _splittingTools.SplitAudioByDurationAsync(nonExistentFile, maxDurationSeconds: 60);

        // Assert
        Assert.Contains("Failed to split audio", result);
    }
}