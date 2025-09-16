using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using FFMpeg.MCP.Host.Models;
using FFMpeg.MCP.Host.Services;
using System.ComponentModel;
using System.Text.Json;
using FFMpeg.MCP.Host.Mcp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FFMpeg.MCP.Host.Tools;

#region Response Models
public class ConversionResponse
{
    public string? Message { get; set; }
    public List<string>? OutputFiles { get; set; }
    public ConversionInfo? Conversion { get; set; }
}

public class ConversionInfo
{
    public string? InputFormat { get; set; }
    public string? OutputFormat { get; set; }
    public long? InputSize { get; set; }
    public long? OutputSize { get; set; }
    public string? InputDuration { get; set; }
    public string? OutputDuration { get; set; }
    public int? Bitrate { get; set; }
    public string? Codec { get; set; }
    public int? CompressionLevel { get; set; }
    public string? CompressionRatio { get; set; }
    public int? SampleRate { get; set; }
    public int? BitDepth { get; set; }
}

public class BatchConversionResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BatchConversionResult>? Results { get; set; }
}

public class BatchConversionResult
{
    public string? InputPath { get; set; }
    public string? OutputPath { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}
#endregion

[McpServerToolType]
public class AudioConversionTools
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<AudioConversionTools> _logger;
    private readonly McpDispatcher _dispatcher;

    public AudioConversionTools(IFFmpegService ffmpegService, ILogger<AudioConversionTools> logger, McpDispatcher dispatcher)
    {
        _ffmpegService = ffmpegService;
        _logger = logger;
        _dispatcher = dispatcher;
    }

    [McpServerTool, Description("Convert audio file to a different format")]
    public Task<McpResponse<ConversionResponse>> ConvertAudioFormatAsync(
        [Description("Full path to the input audio file")] string inputPath,
        [Description("Full path for the output file")] string outputPath,
        [Description("Target format (e.g., mp3, wav, flac, aac, ogg)")] string format,
        [Description("Audio bitrate in kbps (optional)")] int? bitrate = null,
        [Description("Audio codec (optional, will use default for format if not specified)")] string? codec = null)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(inputPath)) throw new ArgumentException("Input path is required.", nameof(inputPath));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path is required.", nameof(outputPath));
            if (!File.Exists(inputPath)) throw new FileNotFoundException("Input audio file not found.", inputPath);

            var options = new ConversionOptions { OutputFormat = format, Bitrate = bitrate, Codec = codec };
            var result = await _ffmpegService.ConvertFileAsync(inputPath, outputPath, options);

            if (!result.Success) throw new InvalidOperationException($"Failed to convert audio: {result.Message}");

            var inputInfo = await _ffmpegService.GetFileInfoAsync(inputPath);
            var outputInfo = await _ffmpegService.GetFileInfoAsync(outputPath);

            return new ConversionResponse
            {
                Message = result.Message,
                OutputFiles = result.OutputFiles,
                Conversion = new ConversionInfo
                {
                    InputFormat = inputInfo?.Format,
                    OutputFormat = format,
                    InputSize = inputInfo?.FileSizeBytes,
                    OutputSize = outputInfo?.FileSizeBytes,
                    InputDuration = inputInfo?.Duration.ToString(@"hh\:mm\:ss"),
                    OutputDuration = outputInfo?.Duration.ToString(@"hh\:mm\:ss"),
                    Bitrate = bitrate,
                    Codec = codec
                }
            };
        });
    }

    [McpServerTool, Description("Batch convert multiple audio files to the same format")]
    public Task<McpResponse<BatchConversionResponse>> BatchConvertAudioAsync(
        [Description("JSON array of input file paths")] string inputPathsJson,
        [Description("Output directory path")] string outputDirectory,
        [Description("Target format (e.g., mp3, wav, flac)")] string format,
        [Description("Audio bitrate in kbps (optional)")] int? bitrate = null,
        [Description("Keep original filenames (true) or generate new ones (false)")] bool keepOriginalNames = true)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            var inputPaths = JsonSerializer.Deserialize<string[]>(inputPathsJson);
            if (inputPaths == null || !inputPaths.Any()) throw new ArgumentException("Invalid or empty input paths JSON provided", nameof(inputPathsJson));

            if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

            var results = new List<BatchConversionResult>();
            var successCount = 0;
            var failureCount = 0;

            foreach (var inputPath in inputPaths)
            {
                try
                {
                    var fileName = keepOriginalNames ? Path.GetFileNameWithoutExtension(inputPath) : $"converted_{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileNameWithoutExtension(inputPath)}";
                    var outputPath = Path.Combine(outputDirectory, $"{fileName}.{format}");
                    var options = new ConversionOptions { OutputFormat = format, Bitrate = bitrate };
                    var result = await _ffmpegService.ConvertFileAsync(inputPath, outputPath, options);

                    results.Add(new BatchConversionResult
                    {
                        InputPath = inputPath,
                        OutputPath = outputPath,
                        Success = result.Success,
                        Message = result.Message
                    });

                    if (result.Success) successCount++;
                    else failureCount++;
                }
                catch (Exception ex)
                {
                    results.Add(new BatchConversionResult { InputPath = inputPath, Success = false, Message = $"Error: {ex.Message}" });
                    failureCount++;
                }
            }

            return new BatchConversionResponse
            {
                Success = successCount > 0,
                Message = $"Batch conversion completed: {successCount} successful, {failureCount} failed",
                TotalFiles = inputPaths.Length,
                SuccessCount = successCount,
                FailureCount = failureCount,
                Results = results
            };
        });
    }
}