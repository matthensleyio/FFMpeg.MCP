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
public class SetChaptersResponse
{
    public string? Message { get; set; }
    public string[]? OutputFiles { get; set; }
    public int ChaptersAdded { get; set; }
    public List<ChapterInfoResponse>? Chapters { get; set; }
}

public class ChapterInfoResponse
{
    public int Index { get; set; }
    public string? Title { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Duration { get; set; }
}

public class GenerateEqualChaptersResponse
{
    public string? Message { get; set; }
    public string[]? OutputFiles { get; set; }
    public int ChaptersGenerated { get; set; }
    public double ChapterDurationMinutes { get; set; }
    public double TotalDurationMinutes { get; set; }
    public List<object>? Chapters { get; set; }
}

public class ExportChapterInfoResponse
{
    public string? Message { get; set; }
    public string? OutputFile { get; set; }
    public int ChaptersExported { get; set; }
    public string? Format { get; set; }
}

public class RemoveChaptersResponse
{
    public string? Message { get; set; }
    public string[]? OutputFiles { get; set; }
}
#endregion

[McpServerToolType]
public class AudioChapterTools
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<AudioChapterTools> _logger;
    private readonly McpDispatcher _dispatcher;

    public AudioChapterTools(IFFmpegService ffmpegService, ILogger<AudioChapterTools> logger, McpDispatcher dispatcher)
    {
        _ffmpegService = ffmpegService;
        _logger = logger;
        _dispatcher = dispatcher;
    }

    [McpServerTool, Description("Add or update chapters in an audio file")]
    public Task<McpResponse<SetChaptersResponse>> SetAudioChaptersAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("JSON array of chapter objects with startTime, endTime, and title")] string chaptersJson,
        [Description("Output file path (optional - will generate if not provided)")] string? outputPath = null)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Audio file not found.", filePath);

            var chaptersData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(chaptersJson);
            if (chaptersData == null || !chaptersData.Any())
                throw new ArgumentException("Invalid or empty chapters JSON provided", nameof(chaptersJson));

            var chapters = new List<ChapterInfo>();
            for (int i = 0; i < chaptersData.Count; i++)
            {
                var chapterData = chaptersData[i];
                var chapter = new ChapterInfo { Index = i };

                if (chapterData.TryGetValue("startTime", out var startValue) &&
                    (TimeSpan.TryParse(startValue.ToString(), out var startTime) ||
                     (double.TryParse(startValue.ToString(), out var startSeconds) && (startTime = TimeSpan.FromSeconds(startSeconds)) != default)))
                {
                    chapter.StartTime = startTime;
                }

                if (chapterData.TryGetValue("endTime", out var endValue) &&
                    (TimeSpan.TryParse(endValue.ToString(), out var endTime) ||
                     (double.TryParse(endValue.ToString(), out var endSeconds) && (endTime = TimeSpan.FromSeconds(endSeconds)) != default)))
                {
                    chapter.EndTime = endTime;
                }

                if (chapterData.TryGetValue("title", out var titleValue))
                {
                    chapter.Title = titleValue.ToString() ?? $"Chapter {i + 1}";
                }

                chapters.Add(chapter);
            }

            var result = await _ffmpegService.SetChaptersAsync(filePath, chapters, outputPath);
            if (!result.Success) throw new InvalidOperationException($"Failed to set chapters: {result.Message}");

            return new SetChaptersResponse
            {
                Message = result.Message,
                OutputFiles = result.OutputFiles.ToArray(),
                ChaptersAdded = chapters.Count,
                Chapters = chapters.Select(c => new ChapterInfoResponse
                {
                    Index = c.Index,
                    Title = c.Title,
                    StartTime = c.StartTime.ToString(@"hh\:mm\:ss"),
                    EndTime = c.EndTime.ToString(@"hh\:mm\:ss"),
                    Duration = (c.EndTime - c.StartTime).ToString(@"hh\:mm\:ss")
                }).ToList()
            };
        });
    }

    [McpServerTool, Description("Generate chapters based on equal time intervals")]
    public Task<McpResponse<GenerateEqualChaptersResponse>> GenerateEqualChaptersAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Duration of each chapter in minutes")] double chapterDurationMinutes,
        [Description("Chapter title pattern (use {index} for chapter number)")] string titlePattern = "Chapter {index}",
        [Description("Output file path (optional - will generate if not provided)")] string? outputPath = null)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Audio file not found.", filePath);

            var fileInfo = await _ffmpegService.GetFileInfoAsync(filePath);
            if (fileInfo == null) throw new InvalidOperationException($"Could not analyze audio file: {filePath}");

            var chapterDuration = TimeSpan.FromMinutes(chapterDurationMinutes);
            var totalDuration = fileInfo.Duration;
            var chapterCount = (int)Math.Ceiling(totalDuration.TotalMinutes / chapterDurationMinutes);

            var chapters = new List<ChapterInfo>();
            for (int i = 0; i < chapterCount; i++)
            {
                chapters.Add(new ChapterInfo
                {
                    Index = i,
                    StartTime = TimeSpan.FromMinutes(i * chapterDurationMinutes),
                    EndTime = i == chapterCount - 1 ? totalDuration : TimeSpan.FromMinutes((i + 1) * chapterDurationMinutes),
                    Title = titlePattern.Replace("{index}", (i + 1).ToString())
                });
            }

            var result = await _ffmpegService.SetChaptersAsync(filePath, chapters, outputPath);
            if (!result.Success) throw new InvalidOperationException($"Failed to generate chapters: {result.Message}");

            return new GenerateEqualChaptersResponse
            {
                Message = result.Message,
                OutputFiles = result.OutputFiles.ToArray(),
                ChaptersGenerated = chapters.Count,
                ChapterDurationMinutes = chapterDurationMinutes,
                TotalDurationMinutes = totalDuration.TotalMinutes,
                Chapters = chapters.Select(c => new
                {
                    index = c.Index + 1,
                    title = c.Title,
                    startTime = c.StartTime.ToString(@"hh\:mm\:ss"),
                    endTime = c.EndTime.ToString(@"hh\:mm\:ss"),
                    durationMinutes = Math.Round((c.EndTime - c.StartTime).TotalMinutes, 2)
                }).Cast<object>().ToList()
            };
        });
    }

    [McpServerTool, Description("Create chapters based on silence detection")]
    public Task<McpResponse<object>> GenerateChaptersBySilenceAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Silence threshold in dB (negative value, e.g., -30)")] double silenceThreshold = -30.0,
        [Description("Minimum silence duration in seconds")] double minSilenceDuration = 2.0,
        [Description("Chapter title pattern (use {index} for chapter number)")] string titlePattern = "Chapter {index}",
        [Description("Output file path (optional - will generate if not provided)")] string? outputPath = null)
    {
        return _dispatcher.DispatchAsync<object>(() =>
        {
            throw new NotImplementedException("Silence-based chapter generation is not yet fully implemented");
        });
    }

    [McpServerTool, Description("Remove all chapters from an audio file")]
    public Task<McpResponse<RemoveChaptersResponse>> RemoveChaptersAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Output file path (optional - will generate if not provided)")] string? outputPath = null)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Audio file not found.", filePath);

            outputPath ??= GenerateOutputPath(filePath, "_no_chapters");
            var options = new ConversionOptions { CustomOptions = new Dictionary<string, string> { ["-map_chapters"] = "-1" } };
            var result = await _ffmpegService.ConvertFileAsync(filePath, outputPath, options);

            if (!result.Success) throw new InvalidOperationException($"Failed to remove chapters: {result.Message} - {result.ErrorDetails}");

            return new RemoveChaptersResponse
            {
                Message = "Chapters removed successfully",
                OutputFiles = result.OutputFiles.ToArray()
            };
        });
    }

    [McpServerTool, Description("Export chapter information to various formats")]
    public Task<McpResponse<ExportChapterInfoResponse>> ExportChapterInfoAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Export format: json, csv, txt, cue")] string format = "json",
        [Description("Output file path (optional - will generate if not provided)")] string? outputPath = null)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Audio file not found.", filePath);

            var fileInfo = await _ffmpegService.GetFileInfoAsync(filePath);
            if (fileInfo == null) throw new InvalidOperationException($"Could not analyze audio file: {filePath}");
            if (!fileInfo.Chapters.Any()) throw new InvalidOperationException("No chapters found in the audio file");

            outputPath ??= GenerateChapterExportPath(filePath, format);
            string content = format.ToLower() switch
            {
                "json" => JsonSerializer.Serialize(fileInfo.Chapters.Select(c => new { c.Index, c.Title, c.StartTime, c.EndTime, c.Metadata }), new JsonSerializerOptions { WriteIndented = true }),
                "csv" => "Index,Title,StartTime,EndTime\n" + string.Join("\n", fileInfo.Chapters.Select(c => $"{c.Index},\"{c.Title}\",{c.StartTime},{c.EndTime}")),
                _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
            };

            await File.WriteAllTextAsync(outputPath, content);

            return new ExportChapterInfoResponse
            {
                Message = $"Chapter information exported to {format.ToUpper()} format",
                OutputFile = outputPath,
                ChaptersExported = fileInfo.Chapters.Count,
                Format = format.ToUpper()
            };
        });
    }

    private static string GenerateOutputPath(string inputPath, string suffix)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);
        return Path.Combine(directory, $"{fileNameWithoutExtension}{suffix}{extension}");
    }

    private static string GenerateChapterExportPath(string inputPath, string format)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(directory, $"{fileNameWithoutExtension}_chapters.{format.ToLower()}");
    }
}