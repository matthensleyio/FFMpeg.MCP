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
using FFMpeg.MCP.Host.Models.Output;

namespace FFMpeg.MCP.Host.Tools;

// Response models have been moved to FFMpeg.MCP.Host.Models.Output

[McpServerToolType]
public class AudioMetadataTools
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<AudioMetadataTools> _logger;
    private readonly McpDispatcher _dispatcher;

    public AudioMetadataTools(IFFmpegService ffmpegService, ILogger<AudioMetadataTools> logger, McpDispatcher dispatcher)
    {
        _ffmpegService = ffmpegService;
        _logger = logger;
        _dispatcher = dispatcher;
    }

    [McpServerTool, Description("Get comprehensive metadata and information about an audio file")]
    public Task<McpResponse<AudioFileInfo>> GetAudioFileInfoAsync(
        [Description("Full path to the audio file")] string filePath)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) { throw new ArgumentException("File path is required.", nameof(filePath)); }
            if (!File.Exists(filePath)) { throw new FileNotFoundException("Audio file not found.", filePath); }

            var fileInfo = await _ffmpegService.GetFileInfoAsync(filePath);
            if (fileInfo == null) { throw new InvalidOperationException($"Could not analyze audio file: {filePath}"); }

            return fileInfo;
        });
    }

    [McpServerTool, Description("Update metadata tags for an audio file")]
    public Task<McpResponse<UpdateMetadataResponse>> UpdateAudioMetadataAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("JSON object containing metadata key-value pairs")] string metadataJson,
        [Description("Output file path (optional - will generate if not provided)")] string? outputPath = null)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) { throw new ArgumentException("File path is required.", nameof(filePath)); }
            if (!File.Exists(filePath)) { throw new FileNotFoundException("Audio file not found.", filePath); }

            var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson);
            if (metadata == null) { throw new ArgumentException("Invalid metadata JSON provided", nameof(metadataJson)); }

            var result = await _ffmpegService.UpdateMetadataAsync(filePath, metadata, outputPath);
            if (!result.Success) { throw new InvalidOperationException($"Failed to update metadata: {result.Message}"); }

            return new UpdateMetadataResponse
            {
                Message = result.Message,
                OutputFiles = result.OutputFiles.ToArray()
            };
        });
    }

    [McpServerTool, Description("Update specific common metadata fields for an audio file")]
    public Task<McpResponse<UpdateMetadataResponse>> UpdateCommonMetadataAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Title of the audio")] string? title = null,
        [Description("Artist/author name")] string? artist = null,
        [Description("Album name")] string? album = null,
        [Description("Genre")] string? genre = null,
        [Description("Year")] string? year = null,
        [Description("Track number")] string? track = null,
        [Description("Comment")] string? comment = null,
        [Description("Output file path (optional)")] string? outputPath = null)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) { throw new ArgumentException("File path is required.", nameof(filePath)); }
            if (!File.Exists(filePath)) { throw new FileNotFoundException("Audio file not found.", filePath); }

            var metadata = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(title)) { metadata["title"] = title; }
            if (!string.IsNullOrEmpty(artist)) { metadata["artist"] = artist; }
            if (!string.IsNullOrEmpty(album)) { metadata["album"] = album; }
            if (!string.IsNullOrEmpty(genre)) { metadata["genre"] = genre; }
            if (!string.IsNullOrEmpty(year)) { metadata["date"] = year; }
            if (!string.IsNullOrEmpty(track)) { metadata["track"] = track; }
            if (!string.IsNullOrEmpty(comment)) { metadata["comment"] = comment; }

            if (!metadata.Any()) { throw new ArgumentException("No metadata fields provided to update"); }

            var result = await _ffmpegService.UpdateMetadataAsync(filePath, metadata, outputPath);
            if (!result.Success) { throw new InvalidOperationException($"Failed to update metadata: {result.Message}"); }

            return new UpdateMetadataResponse
            {
                Message = result.Message,
                UpdatedFields = metadata.Keys.ToArray(),
                OutputFiles = result.OutputFiles.ToArray()
            };
        });
    }

    [McpServerTool, Description("List all supported audio formats")]
    public Task<McpResponse<SupportedFormatsResponse>> GetSupportedFormatsAsync()
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            var formats = await _ffmpegService.GetSupportedFormatsAsync();
            return new SupportedFormatsResponse
            {
                SupportedFormats = formats,
                Count = formats.Count
            };
        });
    }

    [McpServerTool, Description("Check if FFmpeg is available and working")]
    public Task<McpResponse<FFmpegAvailabilityResponse>> CheckFFmpegAvailabilityAsync()
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            var isAvailable = await _ffmpegService.IsFFmpegAvailableAsync();
            return new FFmpegAvailabilityResponse
            {
                FfmpegAvailable = isAvailable,
                Message = isAvailable ? "FFmpeg is available and working" : "FFmpeg is not available or not working"
            };
        });
    }
}