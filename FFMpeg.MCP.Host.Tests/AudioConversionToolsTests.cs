using FFMpeg.MCP.Host.Tools;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tests;

public class AudioConversionToolsTests : TestBase
{
    private readonly AudioConversionTools _conversionTools;

    public AudioConversionToolsTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AudioConversionTools>();
        _conversionTools = new AudioConversionTools(FFmpegService, logger);
    }

    [Fact]
    public async Task ConvertAudioFormatAsync_Mp3ToWav_CreatesWavFile()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");
        var outputFile = GetWorkingPath("converted.wav");

        // Act
        var result = await _conversionTools.ConvertAudioFormatAsync(inputFile, outputFile, "wav", bitrate: null, codec: null);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            AssertFileExists(outputFile);
            Assert.Equal(".wav", Path.GetExtension(outputFile).ToLower());
        }
    }

    [Fact]
    public async Task ConvertToMp3Async_WithHighQuality_CreatesMp3()
    {
        // Arrange
        var inputFile = CopyTestFile("long-speech.wav");
        var outputFile = GetWorkingPath("high-quality.mp3");

        // Act
        var result = await _conversionTools.ConvertToMp3Async(inputFile, outputFile, quality: "high");

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            AssertFileExists(outputFile);
            var conversion = response.GetProperty("conversion");
            Assert.Equal("mp3", conversion.GetProperty("outputFormat").GetString());
            Assert.Equal(320, conversion.GetProperty("bitrate").GetInt32());
        }
    }

    [Fact]
    public async Task ConvertToMp3Async_WithMediumQuality_CreatesMp3()
    {
        // Arrange
        var inputFile = CopyTestFile("long-speech.wav");
        var outputFile = GetWorkingPath("medium-quality.mp3");

        // Act
        var result = await _conversionTools.ConvertToMp3Async(inputFile, outputFile, quality: "medium");

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            AssertFileExists(outputFile);
            var conversion = response.GetProperty("conversion");
            Assert.Equal(192, conversion.GetProperty("bitrate").GetInt32());
        }
    }

    [Fact]
    public async Task ConvertToFlacAsync_WithValidFile_CreatesFlacFile()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");
        var outputFile = GetWorkingPath("lossless.flac");

        // Act
        var result = await _conversionTools.ConvertToFlacAsync(inputFile, outputFile, compressionLevel: 5);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            AssertFileExists(outputFile);
            Assert.Equal(".flac", Path.GetExtension(outputFile).ToLower());

            var conversion = response.GetProperty("conversion");
            Assert.Equal("FLAC (lossless)", conversion.GetProperty("format").GetString());
            Assert.Equal(5, conversion.GetProperty("compressionLevel").GetInt32());
        }
    }

    [Fact]
    public async Task ConvertToWavAsync_WithCustomSampleRate_CreatesWavFile()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");
        var outputFile = GetWorkingPath("custom-wav.wav");

        // Act
        var result = await _conversionTools.ConvertToWavAsync(inputFile, outputFile, sampleRate: 48000, bitDepth: 24);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            AssertFileExists(outputFile);
            var conversion = response.GetProperty("conversion");
            Assert.Equal("WAV (uncompressed)", conversion.GetProperty("format").GetString());
            Assert.Equal(48000, conversion.GetProperty("sampleRate").GetInt32());
            Assert.Equal(24, conversion.GetProperty("bitDepth").GetInt32());
        }
    }

    [Fact]
    public async Task ConvertToAacAsync_WithStandardSettings_CreatesAacFile()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");
        var outputFile = GetWorkingPath("output.aac");

        // Act
        var result = await _conversionTools.ConvertToAacAsync(inputFile, outputFile, bitrate: 128, useHEAAC: false);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            AssertFileExists(outputFile);
            var conversion = response.GetProperty("conversion");
            Assert.Equal("AAC", conversion.GetProperty("format").GetString());
            Assert.Equal(128, conversion.GetProperty("bitrate").GetInt32());
        }
    }

    [Fact]
    public async Task ConvertAudioAdvancedAsync_WithCustomOptions_ConvertsFile()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");
        var outputFile = GetWorkingPath("advanced.wav");
        var options = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["format"] = "wav",
            ["bitrate"] = 256,
            ["customOptions"] = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["-ar"] = "44100"
            })
        });

        // Act
        var result = await _conversionTools.ConvertAudioAdvancedAsync(inputFile, outputFile, options);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            AssertFileExists(outputFile);
            var conversion = response.GetProperty("conversion");
            Assert.Equal("wav", conversion.GetProperty("format").GetString());
        }
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
        var result = await _conversionTools.BatchConvertAudioAsync(inputPaths, outputDir, "wav", bitrate: null, keepOriginalNames: true);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            Assert.True(response.GetProperty("successCount").GetInt32() > 0);

            var results = response.GetProperty("results").EnumerateArray();
            foreach (var fileResult in results)
            {
                if (fileResult.GetProperty("success").GetBoolean())
                {
                    var outputPath = fileResult.GetProperty("outputPath").GetString();
                    if (!string.IsNullOrEmpty(outputPath))
                    {
                        TrackCreatedFile(outputPath);
                        AssertFileExists(outputPath);
                        Assert.Equal(".wav", Path.GetExtension(outputPath).ToLower());
                    }
                }
            }
        }
    }

    [Fact]
    public async Task ConvertAudioFormatAsync_WithNonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentFile = GetWorkingPath("nonexistent.mp3");
        var outputFile = GetWorkingPath("output.wav");

        // Act
        var result = await _conversionTools.ConvertAudioFormatAsync(nonExistentFile, outputFile, "wav");

        // Assert
        Assert.Contains("File not found", result);
    }

    [Fact]
    public async Task BatchConvertAudioAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var outputDir = Path.Combine(WorkingDirectory, "batch-output");

        // Act
        var result = await _conversionTools.BatchConvertAudioAsync(invalidJson, outputDir, "mp3");

        // Assert
        Assert.Contains("Invalid or empty input paths JSON provided", result);
    }

    [Fact]
    public async Task ConvertAudioAdvancedAsync_WithInvalidOptions_ReturnsError()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");
        var outputFile = GetWorkingPath("output.wav");
        var invalidOptions = "{ invalid json }";

        // Act
        var result = await _conversionTools.ConvertAudioAdvancedAsync(inputFile, outputFile, invalidOptions);

        // Assert
        Assert.Contains("Invalid options JSON provided", result);
    }
}