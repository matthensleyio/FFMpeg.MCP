using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using FFMpeg.MCP.Host.Models;
using FFMpeg.MCP.Host.Services;
using System.ComponentModel;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tools;

[McpServerToolType]
public class AudioSplittingTools
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<AudioSplittingTools> _logger;

    public AudioSplittingTools(IFFmpegService ffmpegService, ILogger<AudioSplittingTools> logger)
    {
        _ffmpegService = ffmpegService;
        _logger = logger;
    }

    [McpServerTool, Description("Split an audio file by existing chapters")]
    public async Task<string> SplitAudioByChaptersAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Output filename pattern (optional). Use {filename}, {chapter}, {title} placeholders")] string? outputPattern = null,
        [Description("Whether to preserve metadata in split files")] bool preserveMetadata = true)
    {
        try
        {
            var options = new SplitOptions
            {
                SplitByChapters = true,
                OutputPattern = outputPattern,
                PreserveMetadata = preserveMetadata
            };

            var result = await _ffmpegService.SplitFileAsync(filePath, options);

            if (result.Success)
            {
                var response = new
                {
                    success = true,
                    message = result.Message,
                    outputFiles = result.OutputFiles,
                    filesCreated = result.OutputFiles.Count
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return $"Failed to split audio by chapters: {result.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error splitting audio by chapters for {FilePath}", filePath);
            return $"Error splitting audio: {ex.Message}";
        }
    }

    [McpServerTool, Description("Split an audio file into segments of specified duration")]
    public async Task<string> SplitAudioByDurationAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Maximum duration for each segment in seconds")] int maxDurationSeconds,
        [Description("Output filename pattern (optional). Use {filename}, {segment} placeholders")] string? outputPattern = null,
        [Description("Whether to preserve metadata in split files")] bool preserveMetadata = true)
    {
        try
        {
            var options = new SplitOptions
            {
                MaxDuration = TimeSpan.FromSeconds(maxDurationSeconds),
                OutputPattern = outputPattern,
                PreserveMetadata = preserveMetadata
            };

            var result = await _ffmpegService.SplitFileAsync(filePath, options);

            if (result.Success)
            {
                var response = new
                {
                    success = true,
                    message = result.Message,
                    outputFiles = result.OutputFiles,
                    filesCreated = result.OutputFiles.Count,
                    maxDurationSeconds = maxDurationSeconds
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return $"Failed to split audio by duration: {result.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error splitting audio by duration for {FilePath}", filePath);
            return $"Error splitting audio: {ex.Message}";
        }
    }

    [McpServerTool, Description("Split an audio file into segments of specified duration (using minutes for convenience)")]
    public async Task<string> SplitAudioByMinutesAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Maximum duration for each segment in minutes")] double maxDurationMinutes,
        [Description("Output filename pattern (optional). Use {filename}, {segment} placeholders")] string? outputPattern = null,
        [Description("Whether to preserve metadata in split files")] bool preserveMetadata = true)
    {
        try
        {
            var maxDurationSeconds = (int)(maxDurationMinutes * 60);
            return await SplitAudioByDurationAsync(filePath, maxDurationSeconds, outputPattern, preserveMetadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error splitting audio by minutes for {FilePath}", filePath);
            return $"Error splitting audio: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get chapter information from an audio file")]
    public async Task<string> GetAudioChaptersAsync(
        [Description("Full path to the audio file")] string filePath)
    {
        try
        {
            var fileInfo = await _ffmpegService.GetFileInfoAsync(filePath);
            if (fileInfo == null)
                return $"Could not analyze audio file: {filePath}";

            var response = new
            {
                filePath = filePath,
                hasChapters = fileInfo.Chapters.Any(),
                chapterCount = fileInfo.Chapters.Count,
                chapters = fileInfo.Chapters.Select(c => new
                {
                    index = c.Index,
                    title = c.Title,
                    startTime = c.StartTime.ToString(@"hh\:mm\:ss"),
                    endTime = c.EndTime.ToString(@"hh\:mm\:ss"),
                    duration = (c.EndTime - c.StartTime).ToString(@"hh\:mm\:ss"),
                    metadata = c.Metadata
                }).ToList()
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chapters for {FilePath}", filePath);
            return $"Error retrieving chapters: {ex.Message}";
        }
    }

    [McpServerTool, Description("Split audio file with advanced options")]
    public async Task<string> SplitAudioAdvancedAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("JSON object with split options")] string optionsJson)
    {
        try
        {
            var optionsData = JsonSerializer.Deserialize<Dictionary<string, object>>(optionsJson);
            if (optionsData == null)
                return "Invalid options JSON provided";

            var options = new SplitOptions
            {
                PreserveMetadata = true
            };

            if (optionsData.ContainsKey("maxDurationSeconds"))
            {
                if (double.TryParse(optionsData["maxDurationSeconds"].ToString(), out var seconds))
                {
                    options.MaxDuration = TimeSpan.FromSeconds(seconds);
                }
            }

            if (optionsData.ContainsKey("maxDurationMinutes"))
            {
                if (double.TryParse(optionsData["maxDurationMinutes"].ToString(), out var minutes))
                {
                    options.MaxDuration = TimeSpan.FromMinutes(minutes);
                }
            }

            if (optionsData.ContainsKey("splitByChapters"))
            {
                if (bool.TryParse(optionsData["splitByChapters"].ToString(), out var splitByChapters))
                {
                    options.SplitByChapters = splitByChapters;
                }
            }

            if (optionsData.ContainsKey("outputPattern"))
            {
                options.OutputPattern = optionsData["outputPattern"].ToString();
            }

            if (optionsData.ContainsKey("preserveMetadata"))
            {
                if (bool.TryParse(optionsData["preserveMetadata"].ToString(), out var preserveMetadata))
                {
                    options.PreserveMetadata = preserveMetadata;
                }
            }

            var result = await _ffmpegService.SplitFileAsync(filePath, options);

            if (result.Success)
            {
                var response = new
                {
                    success = true,
                    message = result.Message,
                    outputFiles = result.OutputFiles,
                    filesCreated = result.OutputFiles.Count,
                    options = new
                    {
                        maxDuration = options.MaxDuration?.ToString(@"hh\:mm\:ss"),
                        splitByChapters = options.SplitByChapters,
                        outputPattern = options.OutputPattern,
                        preserveMetadata = options.PreserveMetadata
                    }
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return $"Failed to split audio: {result.Message} - {result.ErrorDetails}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error splitting audio with advanced options for {FilePath}", filePath);
            return $"Error splitting audio: {ex.Message}";
        }
    }
}