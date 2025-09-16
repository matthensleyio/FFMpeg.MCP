using FFMpeg.MCP.Host.Mcp;
using FFMpeg.MCP.Host.Tools;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tests;

public class AudioConversionToolsTests : TestBase
{
    private readonly AudioConversionTools _conversionTools;

    public AudioConversionToolsTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dispatcherLogger = loggerFactory.CreateLogger<McpDispatcher>();
        var dispatcher = new McpDispatcher(dispatcherLogger);
        _conversionTools = new AudioConversionTools(FFmpegService, loggerFactory.CreateLogger<AudioConversionTools>(), dispatcher);
    }

    [Fact]
    public async Task ConvertAudioFormatAsync_Mp3ToWav_CreatesWavFile()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");
        var outputFile = GetWorkingPath("converted.wav");

        // Act
        var response = await _conversionTools.ConvertAudioFormatAsync(inputFile, outputFile, "wav");

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        AssertFileExists(outputFile);
    }

    [Fact]
    public async Task BatchConvertAudioAsync_WithMultipleFiles_ConvertsAllFiles()
    {
        // Arrange
        var inputFile1 = CopyTestFile("sample-short.mp3");
        var inputFile2 = CopyTestFile("long-speech.wav");
        var inputPaths = JsonSerializer.Serialize(new[] { inputFile1, inputFile2 });
        var outputDir = Path.Combine(WorkingDirectory, "batch-output");
        Directory.CreateDirectory(outputDir);
        TrackCreatedDirectory(outputDir);

        // Act
        var response = await _conversionTools.BatchConvertAudioAsync(inputPaths, outputDir, "wav");

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        Assert.True(response.Result.SuccessCount > 0);
    }

    [Fact]
    public async Task ConvertAudioFormatAsync_WithNonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentFile = GetWorkingPath("nonexistent.mp3");
        var outputFile = GetWorkingPath("output.wav");

        // Act
        var response = await _conversionTools.ConvertAudioFormatAsync(nonExistentFile, outputFile, "wav");

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal(McpErrorCodes.NotFound, response.Error.Code);
    }

    [Fact]
    public async Task BatchConvertAudioAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var outputDir = Path.Combine(WorkingDirectory, "batch-output");

        // Act
        var response = await _conversionTools.BatchConvertAudioAsync(invalidJson, outputDir, "mp3");

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal(McpErrorCodes.InternalError, response.Error.Code);
    }
}