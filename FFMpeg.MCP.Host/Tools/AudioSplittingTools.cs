using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using FFMpeg.MCP.Host.Models.Output;
using FFMpeg.MCP.Host.Models.Input;
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

// Response models have been moved to FFMpeg.MCP.Host.Models.Output

[McpServerToolType]
public class AudioSplittingTools
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<AudioSplittingTools> _logger;
    private readonly McpDispatcher _dispatcher;
    private readonly IProgressReporter _progressReporter;

    public AudioSplittingTools(IFFmpegService ffmpegService, ILogger<AudioSplittingTools> logger, McpDispatcher dispatcher, IProgressReporter progressReporter)
    {
        _ffmpegService = ffmpegService;
        _logger = logger;
        _dispatcher = dispatcher;
        _progressReporter = progressReporter;
    }

    [McpServerTool, Description("Split an audio file by existing chapters with progress tracking")]
    public Task<McpResponse<SplitWithProgressResponse>> SplitAudioByChaptersAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Output filename pattern (optional). Use {filename}, {chapter}, {title} placeholders")] string? outputPattern = null,
        [Description("Whether to preserve metadata in split files")] bool preserveMetadata = true)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Audio file not found.", filePath);

            var fileInfo = await _ffmpegService.GetFileInfoAsync(filePath);
            if (fileInfo == null) throw new InvalidOperationException($"Could not analyze audio file: {filePath}");

            if (!fileInfo.Chapters.Any()) throw new InvalidOperationException("No chapters found in the audio file.");

            var options = new SplitOptions { SplitByChapters = true, OutputPattern = outputPattern, PreserveMetadata = preserveMetadata };

            var startResult = await _ffmpegService.SplitFileAsyncWithProgress(filePath, options, _progressReporter);

            var message = startResult.IsNewOperation
                ? $"Started splitting {fileInfo.Chapters.Count} chapters. Use the operation ID to monitor progress."
                : $"Found existing operation splitting {fileInfo.Chapters.Count} chapters. Use the operation ID to monitor progress.";

            return new SplitWithProgressResponse
            {
                OperationId = startResult.OperationId,
                Message = message,
                TotalChapters = fileInfo.Chapters.Count,
                IsNewOperation = startResult.IsNewOperation
            };
        });
    }

    [McpServerTool, Description("Split an audio file into segments of specified duration with progress tracking")]
    public Task<McpResponse<SplitWithProgressResponse>> SplitAudioByDurationAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Maximum duration for each segment in seconds")] int maxDurationSeconds,
        [Description("Output filename pattern (optional). Use {filename}, {segment} placeholders")] string? outputPattern = null,
        [Description("Whether to preserve metadata in split files")] bool preserveMetadata = true)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Audio file not found.", filePath);

            var fileInfo = await _ffmpegService.GetFileInfoAsync(filePath);
            if (fileInfo == null) throw new InvalidOperationException($"Could not analyze audio file: {filePath}");

            var maxDuration = TimeSpan.FromSeconds(maxDurationSeconds);
            var totalSegments = (int)Math.Ceiling(fileInfo.Duration.TotalSeconds / maxDuration.TotalSeconds);

            var options = new SplitOptions { MaxDuration = maxDuration, OutputPattern = outputPattern, PreserveMetadata = preserveMetadata };

            var startResult = await _ffmpegService.SplitFileAsyncWithProgress(filePath, options, _progressReporter);

            var message = startResult.IsNewOperation
                ? $"Started splitting into {totalSegments} segments. Use the operation ID to monitor progress."
                : $"Found existing operation splitting into {totalSegments} segments. Use the operation ID to monitor progress.";

            return new SplitWithProgressResponse
            {
                OperationId = startResult.OperationId,
                Message = message,
                TotalChapters = totalSegments,
                IsNewOperation = startResult.IsNewOperation
            };
        });
    }

    [McpServerTool, Description("Split an audio file into segments of specified duration (using minutes for convenience) with progress tracking")]
    public Task<McpResponse<SplitWithProgressResponse>> SplitAudioByMinutesAsync(
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

    [McpServerTool, Description("Get the current progress of a long-running operation")]
    public Task<McpResponse<OperationProgress?>> GetOperationProgressAsync(
        [Description("Operation ID returned from a progress-aware operation")] string operationId)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(operationId)) throw new ArgumentException("Operation ID is required.", nameof(operationId));

            return await _progressReporter.GetProgressAsync(operationId);
        });
    }
}