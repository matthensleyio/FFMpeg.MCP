using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.Extensions.Logging;
using FFMpeg.MCP.Host.Models;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Services;

public interface IFFmpegService
{
    Task<AudioFileInfo?> GetFileInfoAsync(string filePath);
    Task<OperationResult> UpdateMetadataAsync(string filePath, Dictionary<string, string> metadata, string? outputPath = null);
    Task<OperationResult> SplitFileAsync(string filePath, SplitOptions options);
    Task<OperationResult> ConvertFileAsync(string filePath, string outputPath, ConversionOptions options);
    Task<OperationResult> SetChaptersAsync(string filePath, List<ChapterInfo> chapters, string? outputPath = null);
    Task<OperationResult> BackupFileAsync(string filePath, string? backupPath = null);
    Task<List<string>> GetSupportedFormatsAsync();
    Task<bool> IsFFmpegAvailableAsync();
}

public class FFmpegService : IFFmpegService
{
    private readonly ILogger<FFmpegService> _logger;

    public FFmpegService(ILogger<FFmpegService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsFFmpegAvailableAsync()
    {
        try
        {
            // Simple check by trying to get the version - this will fail if FFmpeg is not available
            await Task.Run(() =>
            {
                var ffmpegPath = FFMpegCore.GlobalFFOptions.Current.BinaryFolder;
                return true;
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AudioFileInfo?> GetFileInfoAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Getting file info for: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            // Set FFmpeg and FFprobe binaries path
            GlobalFFOptions.Configure(options =>
            {
                options.BinaryFolder = Path.Combine(AppContext.BaseDirectory, "assets", "ffmpeg");
            });

            var mediaInfo = await FFProbe.AnalyseAsync(filePath);

            var audioFileInfo = new AudioFileInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Duration = mediaInfo.Duration,
                Format = mediaInfo.Format.FormatName,
                FileSizeBytes = new FileInfo(filePath).Length
            };

            // Extract metadata
            if (mediaInfo.Format.Tags != null)
            {
                foreach (var tag in mediaInfo.Format.Tags)
                {
                    audioFileInfo.Metadata[tag.Key] = tag.Value;
                }
            }

            _logger.LogInformation("Successfully retrieved file info for: {FilePath}", filePath);
            return audioFileInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info for: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<OperationResult> UpdateMetadataAsync(string filePath, Dictionary<string, string> metadata, string? outputPath = null)
    {
        try
        {
            _logger.LogInformation("Updating metadata for: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"File not found: {filePath}"
                };
            }

            outputPath ??= GenerateOutputPath(filePath, "_updated");

            var arguments = FFMpegArguments.FromFileInput(filePath);

            var success = await arguments
                .OutputToFile(outputPath, false, outputOptions =>
                {
                    foreach (var kvp in metadata)
                    {
                        outputOptions.WithCustomArgument($"-metadata {kvp.Key}=\"{kvp.Value}\"");
                    }
                })
                .ProcessAsynchronously();

            if (success)
            {
                _logger.LogInformation("Successfully updated metadata for: {FilePath}", filePath);
                return new OperationResult
                {
                    Success = true,
                    Message = "Metadata updated successfully",
                    OutputFiles = [outputPath]
                };
            }

            return new OperationResult
            {
                Success = false,
                Message = "Failed to update metadata"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating metadata for: {FilePath}", filePath);
            return new OperationResult
            {
                Success = false,
                Message = "Error updating metadata",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<OperationResult> SplitFileAsync(string filePath, SplitOptions options)
    {
        try
        {
            _logger.LogInformation("Splitting file: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"File not found: {filePath}"
                };
            }

            var outputFiles = new List<string>();
            var fileInfo = await GetFileInfoAsync(filePath);

            if (fileInfo == null)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "Could not analyze file"
                };
            }

            if (options.SplitByChapters && fileInfo.Chapters.Any())
            {
                // Split by chapters
                for (int i = 0; i < fileInfo.Chapters.Count; i++)
                {
                    var chapter = fileInfo.Chapters[i];
                    var outputPath = GenerateChapterOutputPath(filePath, i + 1, chapter.Title, options.OutputPattern);

                    var success = await FFMpegArguments
                        .FromFileInput(filePath)
                        .OutputToFile(outputPath, false, outputOptions => outputOptions
                            .Seek(chapter.StartTime)
                            .WithDuration(chapter.EndTime - chapter.StartTime)
                            .CopyChannel())
                        .ProcessAsynchronously();

                    if (success)
                    {
                        outputFiles.Add(outputPath);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create chapter file: {OutputPath}", outputPath);
                    }
                }
            }
            else if (options.MaxDuration.HasValue)
            {
                // Split by duration
                var maxDuration = options.MaxDuration.Value;
                var totalDuration = fileInfo.Duration;
                var segmentCount = (int)Math.Ceiling(totalDuration.TotalSeconds / maxDuration.TotalSeconds);

                for (int i = 0; i < segmentCount; i++)
                {
                    var startTime = TimeSpan.FromSeconds(i * maxDuration.TotalSeconds);
                    var duration = i == segmentCount - 1
                        ? totalDuration - startTime
                        : maxDuration;

                    var outputPath = GenerateSegmentOutputPath(filePath, i + 1, options.OutputPattern);

                    var success = await FFMpegArguments
                        .FromFileInput(filePath)
                        .OutputToFile(outputPath, false, outputOptions => outputOptions
                            .Seek(startTime)
                            .WithDuration(duration)
                            .CopyChannel())
                        .ProcessAsynchronously();

                    if (success)
                    {
                        outputFiles.Add(outputPath);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create segment file: {OutputPath}", outputPath);
                    }
                }
            }

            if (outputFiles.Any())
            {
                _logger.LogInformation("Successfully split file into {Count} parts", outputFiles.Count);
                return new OperationResult
                {
                    Success = true,
                    Message = $"File split into {outputFiles.Count} parts",
                    OutputFiles = outputFiles
                };
            }

            return new OperationResult
            {
                Success = false,
                Message = "No split operation performed - please specify either MaxDuration or SplitByChapters"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error splitting file: {FilePath}", filePath);
            return new OperationResult
            {
                Success = false,
                Message = "Error splitting file",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<OperationResult> ConvertFileAsync(string filePath, string outputPath, ConversionOptions options)
    {
        try
        {
            _logger.LogInformation("Converting file: {FilePath} to {OutputPath}", filePath, outputPath);

            if (!File.Exists(filePath))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"File not found: {filePath}"
                };
            }

            var arguments = FFMpegArguments.FromFileInput(filePath);

            var success = await arguments
                .OutputToFile(outputPath, true, outputOptions =>
                {
                    if (!string.IsNullOrEmpty(options.OutputFormat))
                    {
                        outputOptions.ForceFormat(options.OutputFormat);
                    }

                    if (options.Bitrate.HasValue)
                    {
                        outputOptions.WithAudioBitrate(options.Bitrate.Value);
                    }

                    if (!string.IsNullOrEmpty(options.Codec))
                    {
                        outputOptions.WithAudioCodec(options.Codec);
                    }

                    foreach (var customOption in options.CustomOptions)
                    {
                        outputOptions.WithCustomArgument($"{customOption.Key} {customOption.Value}");
                    }
                })
                .ProcessAsynchronously();

            if (success)
            {
                _logger.LogInformation("Successfully converted file: {FilePath}", filePath);
                return new OperationResult
                {
                    Success = true,
                    Message = "File converted successfully",
                    OutputFiles = [outputPath]
                };
            }

            return new OperationResult
            {
                Success = false,
                Message = "Failed to convert file"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting file: {FilePath}", filePath);
            return new OperationResult
            {
                Success = false,
                Message = "Error converting file",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<OperationResult> SetChaptersAsync(string filePath, List<ChapterInfo> chapters, string? outputPath = null)
    {
        try
        {
            _logger.LogInformation("Setting chapters for: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"File not found: {filePath}"
                };
            }

            outputPath ??= GenerateOutputPath(filePath, "_chaptered");

            // Create chapter file
            var chapterFilePath = Path.GetTempFileName();
            var chapterContent = GenerateChapterFile(chapters);
            await File.WriteAllTextAsync(chapterFilePath, chapterContent);

            try
            {
                var success = await FFMpegArguments
                    .FromFileInput(filePath)
                    .OutputToFile(outputPath, false, outputOptions => outputOptions
                        .WithCustomArgument($"-i \"{chapterFilePath}\"")
                        .WithCustomArgument("-map_chapters 1")
                        .CopyChannel())
                    .ProcessAsynchronously();

                if (success)
                {
                    _logger.LogInformation("Successfully set chapters for: {FilePath}", filePath);
                    return new OperationResult
                    {
                        Success = true,
                        Message = "Chapters set successfully",
                        OutputFiles = [outputPath]
                    };
                }

                return new OperationResult
                {
                    Success = false,
                    Message = "Failed to set chapters"
                };
            }
            finally
            {
                // Clean up temporary chapter file
                if (File.Exists(chapterFilePath))
                {
                    File.Delete(chapterFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting chapters for: {FilePath}", filePath);
            return new OperationResult
            {
                Success = false,
                Message = "Error setting chapters",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<OperationResult> BackupFileAsync(string filePath, string? backupPath = null)
    {
        try
        {
            _logger.LogInformation("Creating backup of: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"File not found: {filePath}"
                };
            }

            backupPath ??= GenerateBackupPath(filePath);

            await using var sourceStream = File.OpenRead(filePath);
            await using var destinationStream = File.Create(backupPath);
            await sourceStream.CopyToAsync(destinationStream);

            _logger.LogInformation("Successfully created backup at: {BackupPath}", backupPath);
            return new OperationResult
            {
                Success = true,
                Message = "Backup created successfully",
                OutputFiles = [backupPath]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup for: {FilePath}", filePath);
            return new OperationResult
            {
                Success = false,
                Message = "Error creating backup",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<List<string>> GetSupportedFormatsAsync()
    {
        try
        {
            // Common audio formats supported by FFmpeg
            var formats = new List<string>
            {
                "mp3", "wav", "flac", "aac", "ogg", "m4a", "wma", "opus", "mp4", "mkv"
            };

            return await Task.FromResult(formats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported formats");
            return new List<string>();
        }
    }

    private static string GenerateOutputPath(string inputPath, string suffix)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);
        return Path.Combine(directory, $"{fileNameWithoutExtension}{suffix}{extension}");
    }

    private static string GenerateChapterOutputPath(string inputPath, int chapterNumber, string chapterTitle, string? pattern)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);

        if (!string.IsNullOrEmpty(pattern))
        {
            var fileName = pattern
                .Replace("{filename}", fileNameWithoutExtension)
                .Replace("{chapter}", chapterNumber.ToString("D2"))
                .Replace("{title}", SanitizeFileName(chapterTitle));
            return Path.Combine(directory, $"{fileName}{extension}");
        }

        return Path.Combine(directory, $"{fileNameWithoutExtension}_Chapter{chapterNumber:D2}_{SanitizeFileName(chapterTitle)}{extension}");
    }

    private static string GenerateSegmentOutputPath(string inputPath, int segmentNumber, string? pattern)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);

        if (!string.IsNullOrEmpty(pattern))
        {
            var fileName = pattern
                .Replace("{filename}", fileNameWithoutExtension)
                .Replace("{segment}", segmentNumber.ToString("D2"));
            return Path.Combine(directory, $"{fileName}{extension}");
        }

        return Path.Combine(directory, $"{fileNameWithoutExtension}_Part{segmentNumber:D2}{extension}");
    }

    private static string GenerateBackupPath(string inputPath)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var fileName = Path.GetFileName(inputPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return Path.Combine(directory, $"{fileName}.backup_{timestamp}");
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string GenerateChapterFile(List<ChapterInfo> chapters)
    {
        var content = new System.Text.StringBuilder();
        content.AppendLine(";FFMETADATA1");

        foreach (var chapter in chapters)
        {
            content.AppendLine("[CHAPTER]");
            content.AppendLine("TIMEBASE=1/1000");
            content.AppendLine($"START={chapter.StartTime.TotalMilliseconds:F0}");
            content.AppendLine($"END={chapter.EndTime.TotalMilliseconds:F0}");
            content.AppendLine($"title={chapter.Title}");

            foreach (var metadata in chapter.Metadata)
            {
                content.AppendLine($"{metadata.Key}={metadata.Value}");
            }

            content.AppendLine();
        }

        return content.ToString();
    }
}