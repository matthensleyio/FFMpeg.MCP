using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using FFMpeg.MCP.Host.Models;
using FFMpeg.MCP.Host.Services;
using System.ComponentModel;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tools;

[McpServerToolType]
public class AudioMetadataTools
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<AudioMetadataTools> _logger;

    public AudioMetadataTools(IFFmpegService ffmpegService, ILogger<AudioMetadataTools> logger)
    {
        _ffmpegService = ffmpegService;
        _logger = logger;
    }

    [McpServerTool, Description("Get comprehensive metadata and information about an audio file")]
    public async Task<string> GetAudioFileInfoAsync(
        [Description("Full path to the audio file")] string filePath)
    {
        try
        {
            var fileInfo = await _ffmpegService.GetFileInfoAsync(filePath);
            if (fileInfo == null)
                return $"Could not analyze audio file: {filePath}";

            return JsonSerializer.Serialize(fileInfo, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audio file info for {FilePath}", filePath);
            return $"Error retrieving file information: {ex.Message}";
        }
    }

    [McpServerTool, Description("Update metadata tags for an audio file")]
    public async Task<string> UpdateAudioMetadataAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("JSON object containing metadata key-value pairs")] string metadataJson,
        [Description("Output file path (optional - will generate if not provided)")] string? outputPath = null)
    {
        try
        {
            var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson);
            if (metadata == null)
                return "Invalid metadata JSON provided";

            var result = await _ffmpegService.UpdateMetadataAsync(filePath, metadata, outputPath);

            if (result.Success)
            {
                var response = new
                {
                    success = true,
                    message = result.Message,
                    outputFiles = result.OutputFiles
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return $"Failed to update metadata: {result.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating metadata for {FilePath}", filePath);
            return $"Error updating metadata: {ex.Message}";
        }
    }

    [McpServerTool, Description("Update specific common metadata fields for an audio file")]
    public async Task<string> UpdateCommonMetadataAsync(
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
        try
        {
            var metadata = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(title)) metadata["title"] = title;
            if (!string.IsNullOrEmpty(artist)) metadata["artist"] = artist;
            if (!string.IsNullOrEmpty(album)) metadata["album"] = album;
            if (!string.IsNullOrEmpty(genre)) metadata["genre"] = genre;
            if (!string.IsNullOrEmpty(year)) metadata["date"] = year;
            if (!string.IsNullOrEmpty(track)) metadata["track"] = track;
            if (!string.IsNullOrEmpty(comment)) metadata["comment"] = comment;

            if (!metadata.Any())
                return "No metadata fields provided to update";

            var result = await _ffmpegService.UpdateMetadataAsync(filePath, metadata, outputPath);

            if (result.Success)
            {
                var response = new
                {
                    success = true,
                    message = result.Message,
                    updatedFields = metadata.Keys.ToArray(),
                    outputFiles = result.OutputFiles
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return $"Failed to update metadata: {result.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating common metadata for {FilePath}", filePath);
            return $"Error updating metadata: {ex.Message}";
        }
    }

    [McpServerTool, Description("List all supported audio formats")]
    public async Task<string> GetSupportedFormatsAsync()
    {
        try
        {
            var formats = await _ffmpegService.GetSupportedFormatsAsync();
            var response = new
            {
                supportedFormats = formats,
                count = formats.Count
            };
            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported formats");
            return $"Error retrieving supported formats: {ex.Message}";
        }
    }

    [McpServerTool, Description("Check if FFmpeg is available and working")]
    public async Task<string> CheckFFmpegAvailabilityAsync()
    {
        try
        {
            var isAvailable = await _ffmpegService.IsFFmpegAvailableAsync();
            var response = new
            {
                ffmpegAvailable = isAvailable,
                message = isAvailable ? "FFmpeg is available and working" : "FFmpeg is not available or not working"
            };
            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking FFmpeg availability");
            return $"Error checking FFmpeg availability: {ex.Message}";
        }
    }
}