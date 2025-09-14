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
public class SplitResponse
{
    public string? Message { get; set; }
    public List<string>? OutputFiles { get; set; }
    public int FilesCreated { get; set; }
}

public class GetChaptersResponse
{
    public string? FilePath { get; set; }
    public bool HasChapters { get; set; }
    public int ChapterCount { get; set; }
    public List<ChapterInfo>? Chapters { get; set; }
}
#endregion

[McpServerToolType]
public class AudioSplittingTools
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<AudioSplittingTools> _logger;
    private readonly McpDispatcher _dispatcher;

    public AudioSplittingTools(IFFmpegService ffmpegService, ILogger<AudioSplittingTools> logger, McpDispatcher dispatcher)
    {
        _ffmpegService = ffmpegService;
        _logger = logger;
        _dispatcher = dispatcher;
    }

    [McpServerTool, Description("Split an audio file by existing chapters")]
    public Task<McpResponse<SplitResponse>> SplitAudioByChaptersAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Output filename pattern (optional). Use {filename}, {chapter}, {title} placeholders")] string? outputPattern = null,
        [Description("Whether to preserve metadata in split files")] bool preserveMetadata = true)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Audio file not found.", filePath);

            var options = new SplitOptions { SplitByChapters = true, OutputPattern = outputPattern, PreserveMetadata = preserveMetadata };
            var result = await _ffmpegService.SplitFileAsync(filePath, options);

            if (!result.Success) throw new InvalidOperationException($"Failed to split by chapters: {result.Message}");

            return new SplitResponse
            {
                Message = result.Message,
                OutputFiles = result.OutputFiles,
                FilesCreated = result.OutputFiles.Count
            };
        });
    }

    [McpServerTool, Description("Split an audio file into segments of specified duration")]
    public Task<McpResponse<SplitResponse>> SplitAudioByDurationAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Maximum duration for each segment in seconds")] int maxDurationSeconds,
        [Description("Output filename pattern (optional). Use {filename}, {segment} placeholders")] string? outputPattern = null,
        [Description("Whether to preserve metadata in split files")] bool preserveMetadata = true)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Audio file not found.", filePath);

            var options = new SplitOptions { MaxDuration = TimeSpan.FromSeconds(maxDurationSeconds), OutputPattern = outputPattern, PreserveMetadata = preserveMetadata };
            var result = await _ffmpegService.SplitFileAsync(filePath, options);

            if (!result.Success) throw new InvalidOperationException($"Failed to split by duration: {result.Message}");

            return new SplitResponse
            {
                Message = result.Message,
                OutputFiles = result.OutputFiles,
                FilesCreated = result.OutputFiles.Count
            };
        });
    }

    [McpServerTool, Description("Split an audio file into segments of specified duration (using minutes for convenience)")]
    public Task<McpResponse<SplitResponse>> SplitAudioByMinutesAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Maximum duration for each segment in minutes")] double maxDurationMinutes,
        [Description("Output filename pattern (optional). Use {filename}, {segment} placeholders")] string? outputPattern = null,
        [Description("Whether to preserve metadata in split files")] bool preserveMetadata = true)
    {
        var maxDurationSeconds = (int)(maxDurationMinutes * 60);
        return SplitAudioByDurationAsync(filePath, maxDurationSeconds, outputPattern, preserveMetadata);
    }

    [McpServerTool, Description("Get chapter information from an audio file")]
    public Task<McpResponse<GetChaptersResponse>> GetAudioChaptersAsync(
        [Description("Full path to the audio file")] string filePath)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Audio file not found.", filePath);

            var fileInfo = await _ffmpegService.GetFileInfoAsync(filePath);
            if (fileInfo == null) throw new InvalidOperationException($"Could not analyze audio file: {filePath}");

            return new GetChaptersResponse
            {
                FilePath = filePath,
                HasChapters = fileInfo.Chapters.Any(),
                ChapterCount = fileInfo.Chapters.Count,
                Chapters = fileInfo.Chapters
            };
        });
    }
}