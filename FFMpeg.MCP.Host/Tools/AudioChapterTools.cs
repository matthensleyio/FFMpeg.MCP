using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using FFMpeg.MCP.Host.Models;
using FFMpeg.MCP.Host.Services;
using System.ComponentModel;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tools;

[McpServerToolType]
public class AudioChapterTools
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<AudioChapterTools> _logger;

    public AudioChapterTools(IFFmpegService ffmpegService, ILogger<AudioChapterTools> logger)
    {
        _ffmpegService = ffmpegService;
        _logger = logger;
    }

    [McpServerTool, Description("Add or update chapters in an audio file")]
    public async Task<string> SetAudioChaptersAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("JSON array of chapter objects with startTime, endTime, and title")] string chaptersJson,
        [Description("Output file path (optional - will generate if not provided)")] string? outputPath = null)
    {
        try
        {
            var chaptersData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(chaptersJson);
            if (chaptersData == null || !chaptersData.Any())
                return "Invalid or empty chapters JSON provided";

            var chapters = new List<ChapterInfo>();

            for (int i = 0; i < chaptersData.Count; i++)
            {
                var chapterData = chaptersData[i];
                var chapter = new ChapterInfo { Index = i };

                if (chapterData.ContainsKey("startTime"))
                {
                    if (TimeSpan.TryParse(chapterData["startTime"].ToString(), out var startTime))
                        chapter.StartTime = startTime;
                    else if (double.TryParse(chapterData["startTime"].ToString(), out var startSeconds))
                        chapter.StartTime = TimeSpan.FromSeconds(startSeconds);
                }

                if (chapterData.ContainsKey("endTime"))
                {
                    if (TimeSpan.TryParse(chapterData["endTime"].ToString(), out var endTime))
                        chapter.EndTime = endTime;
                    else if (double.TryParse(chapterData["endTime"].ToString(), out var endSeconds))
                        chapter.EndTime = TimeSpan.FromSeconds(endSeconds);
                }

                if (chapterData.ContainsKey("title"))
                {
                    chapter.Title = chapterData["title"].ToString() ?? $"Chapter {i + 1}";
                }

                if (chapterData.ContainsKey("metadata"))
                {
                    var metadataJson = chapterData["metadata"].ToString();
                    if (!string.IsNullOrEmpty(metadataJson))
                    {
                        var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson);
                        if (metadata != null)
                        {
                            chapter.Metadata = metadata;
                        }
                    }
                }

                chapters.Add(chapter);
            }

            var result = await _ffmpegService.SetChaptersAsync(filePath, chapters, outputPath);

            if (result.Success)
            {
                var response = new
                {
                    success = true,
                    message = result.Message,
                    outputFiles = result.OutputFiles,
                    chaptersAdded = chapters.Count,
                    chapters = chapters.Select(c => new
                    {
                        index = c.Index,
                        title = c.Title,
                        startTime = c.StartTime.ToString(@"hh\:mm\:ss"),
                        endTime = c.EndTime.ToString(@"hh\:mm\:ss"),
                        duration = (c.EndTime - c.StartTime).ToString(@"hh\:mm\:ss")
                    }).ToList()
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return $"Failed to set chapters: {result.Message} - {result.ErrorDetails}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting chapters for {FilePath}", filePath);
            return $"Error setting chapters: {ex.Message}";
        }
    }

    [McpServerTool, Description("Generate chapters based on equal time intervals")]
    public async Task<string> GenerateEqualChaptersAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Duration of each chapter in minutes")] double chapterDurationMinutes,
        [Description("Chapter title pattern (use {index} for chapter number)")] string titlePattern = "Chapter {index}",
        [Description("Output file path (optional - will generate if not provided)")] string? outputPath = null)
    {
        try
        {
            var fileInfo = await _ffmpegService.GetFileInfoAsync(filePath);
            if (fileInfo == null)
                return $"Could not analyze audio file: {filePath}";

            var chapterDuration = TimeSpan.FromMinutes(chapterDurationMinutes);
            var totalDuration = fileInfo.Duration;
            var chapterCount = (int)Math.Ceiling(totalDuration.TotalMinutes / chapterDurationMinutes);

            var chapters = new List<ChapterInfo>();

            for (int i = 0; i < chapterCount; i++)
            {
                var startTime = TimeSpan.FromMinutes(i * chapterDurationMinutes);
                var endTime = i == chapterCount - 1 ? totalDuration : TimeSpan.FromMinutes((i + 1) * chapterDurationMinutes);

                var chapter = new ChapterInfo
                {
                    Index = i,
                    StartTime = startTime,
                    EndTime = endTime,
                    Title = titlePattern.Replace("{index}", (i + 1).ToString())
                };

                chapters.Add(chapter);
            }

            var result = await _ffmpegService.SetChaptersAsync(filePath, chapters, outputPath);

            if (result.Success)
            {
                var response = new
                {
                    success = true,
                    message = result.Message,
                    outputFiles = result.OutputFiles,
                    chaptersGenerated = chapters.Count,
                    chapterDurationMinutes = chapterDurationMinutes,
                    totalDurationMinutes = totalDuration.TotalMinutes,
                    chapters = chapters.Select(c => new
                    {
                        index = c.Index + 1,
                        title = c.Title,
                        startTime = c.StartTime.ToString(@"hh\:mm\:ss"),
                        endTime = c.EndTime.ToString(@"hh\:mm\:ss"),
                        durationMinutes = Math.Round((c.EndTime - c.StartTime).TotalMinutes, 2)
                    }).ToList()
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return $"Failed to generate chapters: {result.Message} - {result.ErrorDetails}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating equal chapters for {FilePath}", filePath);
            return $"Error generating chapters: {ex.Message}";
        }
    }

    [McpServerTool, Description("Create chapters based on silence detection")]
    public async Task<string> GenerateChaptersBySilenceAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Silence threshold in dB (negative value, e.g., -30)")] double silenceThreshold = -30.0,
        [Description("Minimum silence duration in seconds")] double minSilenceDuration = 2.0,
        [Description("Chapter title pattern (use {index} for chapter number)")] string titlePattern = "Chapter {index}",
        [Description("Output file path (optional - will generate if not provided)")] string? outputPath = null)
    {
        try
        {
            // Note: This is a simplified implementation. In practice, you would need to:
            // 1. Use FFmpeg's silencedetect filter to find silence periods
            // 2. Parse the output to get silence timestamps
            // 3. Create chapters based on those timestamps

            var fileInfo = await _ffmpegService.GetFileInfoAsync(filePath);
            if (fileInfo == null)
                return $"Could not analyze audio file: {filePath}";

            // This is a placeholder implementation
            // In a real implementation, you would run FFmpeg with silencedetect filter
            // and parse the output to create chapters

            return JsonSerializer.Serialize(new
            {
                success = false,
                message = "Silence-based chapter generation is not yet fully implemented",
                note = "This feature requires running FFmpeg with the silencedetect filter and parsing the output",
                suggestedImplementation = "Use ffmpeg -i input.mp3 -af silencedetect=noise=-30dB:duration=2 -f null - to detect silence"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chapters by silence for {FilePath}", filePath);
            return $"Error generating chapters: {ex.Message}";
        }
    }

    [McpServerTool, Description("Remove all chapters from an audio file")]
    public async Task<string> RemoveChaptersAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Output file path (optional - will generate if not provided)")] string? outputPath = null)
    {
        try
        {
            outputPath ??= GenerateOutputPath(filePath, "_no_chapters");

            var options = new ConversionOptions
            {
                CustomOptions = new Dictionary<string, string>
                {
                    ["-map_chapters"] = "-1"
                }
            };

            var result = await _ffmpegService.ConvertFileAsync(filePath, outputPath, options);

            if (result.Success)
            {
                var response = new
                {
                    success = true,
                    message = "Chapters removed successfully",
                    outputFiles = result.OutputFiles
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return $"Failed to remove chapters: {result.Message} - {result.ErrorDetails}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing chapters for {FilePath}", filePath);
            return $"Error removing chapters: {ex.Message}";
        }
    }

    [McpServerTool, Description("Export chapter information to various formats")]
    public async Task<string> ExportChapterInfoAsync(
        [Description("Full path to the audio file")] string filePath,
        [Description("Export format: json, csv, txt, cue")] string format = "json",
        [Description("Output file path (optional - will generate if not provided)")] string? outputPath = null)
    {
        try
        {
            var fileInfo = await _ffmpegService.GetFileInfoAsync(filePath);
            if (fileInfo == null)
                return $"Could not analyze audio file: {filePath}";

            if (!fileInfo.Chapters.Any())
                return "No chapters found in the audio file";

            outputPath ??= GenerateChapterExportPath(filePath, format);

            string content;
            switch (format.ToLower())
            {
                case "json":
                    content = JsonSerializer.Serialize(fileInfo.Chapters.Select(c => new
                    {
                        index = c.Index + 1,
                        title = c.Title,
                        startTime = c.StartTime.ToString(@"hh\:mm\:ss\.fff"),
                        endTime = c.EndTime.ToString(@"hh\:mm\:ss\.fff"),
                        startTimeSeconds = c.StartTime.TotalSeconds,
                        endTimeSeconds = c.EndTime.TotalSeconds,
                        durationSeconds = (c.EndTime - c.StartTime).TotalSeconds,
                        metadata = c.Metadata
                    }).ToList(), new JsonSerializerOptions { WriteIndented = true });
                    break;

                case "csv":
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine("Index,Title,StartTime,EndTime,DurationSeconds");
                    foreach (var chapter in fileInfo.Chapters)
                    {
                        csv.AppendLine($"{chapter.Index + 1},\"{chapter.Title}\",{chapter.StartTime:hh\\:mm\\:ss\\.fff},{chapter.EndTime:hh\\:mm\\:ss\\.fff},{(chapter.EndTime - chapter.StartTime).TotalSeconds:F3}");
                    }
                    content = csv.ToString();
                    break;

                case "txt":
                    var txt = new System.Text.StringBuilder();
                    txt.AppendLine($"Chapter Information for: {fileInfo.FileName}");
                    txt.AppendLine($"Total Duration: {fileInfo.Duration:hh\\:mm\\:ss}");
                    txt.AppendLine($"Chapters: {fileInfo.Chapters.Count}");
                    txt.AppendLine();
                    foreach (var chapter in fileInfo.Chapters)
                    {
                        txt.AppendLine($"Chapter {chapter.Index + 1}: {chapter.Title}");
                        txt.AppendLine($"  Time: {chapter.StartTime:hh\\:mm\\:ss} - {chapter.EndTime:hh\\:mm\\:ss}");
                        txt.AppendLine($"  Duration: {chapter.EndTime - chapter.StartTime:hh\\:mm\\:ss}");
                        if (chapter.Metadata.Any())
                        {
                            txt.AppendLine("  Metadata:");
                            foreach (var meta in chapter.Metadata)
                            {
                                txt.AppendLine($"    {meta.Key}: {meta.Value}");
                            }
                        }
                        txt.AppendLine();
                    }
                    content = txt.ToString();
                    break;

                case "cue":
                    var cue = new System.Text.StringBuilder();
                    cue.AppendLine($"TITLE \"{fileInfo.FileName}\"");
                    cue.AppendLine($"FILE \"{fileInfo.FileName}\" WAVE");
                    foreach (var chapter in fileInfo.Chapters)
                    {
                        cue.AppendLine($"  TRACK {chapter.Index + 1:D2} AUDIO");
                        cue.AppendLine($"    TITLE \"{chapter.Title}\"");
                        cue.AppendLine($"    INDEX 01 {chapter.StartTime:mm\\:ss\\:ff}");
                    }
                    content = cue.ToString();
                    break;

                default:
                    return $"Unsupported export format: {format}. Supported formats: json, csv, txt, cue";
            }

            await File.WriteAllTextAsync(outputPath, content);

            var response = new
            {
                success = true,
                message = $"Chapter information exported to {format.ToUpper()} format",
                outputFile = outputPath,
                chaptersExported = fileInfo.Chapters.Count,
                format = format.ToUpper()
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting chapter info for {FilePath}", filePath);
            return $"Error exporting chapter info: {ex.Message}";
        }
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