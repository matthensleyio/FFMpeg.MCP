using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Diagnostics;
using FFMpeg.MCP.Host.Models.Output;
using FFMpeg.MCP.Host.Models.Input;

namespace FFMpeg.MCP.Host.Services;

public interface IFFmpegService
{
    Task<AudioFileInfo?> GetFileInfoAsync(string filePath);
    Task<OperationResult> UpdateMetadataAsync(string filePath, Dictionary<string, string> metadata, string? outputPath = null);
    Task<OperationResult> SplitFileAsync(string filePath, SplitOptions options);
    Task<OperationStartResult> SplitFileAsyncWithProgress(string filePath, SplitOptions options, IProgressReporter? progressReporter = null);
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

    private async Task<ProcessExecutionResult> ExecuteFFmpegProcess(string executable, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            _logger.LogError("FFmpeg process exited with code {ExitCode}: {Error}", process.ExitCode, error);
            return new ProcessExecutionResult { Success = false, Output = output, Error = error };
        }

        return new ProcessExecutionResult { Success = true, Output = output, Error = error };
    }

    public async Task<bool> IsFFmpegAvailableAsync()
    {
        var result = await ExecuteFFmpegProcess("ffmpeg", "-version");
        return result.Success;
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

            var arguments = $"-v quiet -print_format json -show_format -show_streams -show_chapters \"{filePath}\"";
            var result = await ExecuteFFmpegProcess("ffprobe", arguments);

            if (!result.Success)
            {
                _logger.LogError("ffprobe failed for {FilePath}: {Error}", filePath, result.Error);
                return null;
            }

            var ffprobeResult = JsonSerializer.Deserialize<FFProbeResult>(result.Output);

            if (ffprobeResult?.Format == null)
            {
                _logger.LogWarning("Could not parse ffprobe output for {FilePath}", filePath);
                return null;
            }

            var audioFileInfo = new AudioFileInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Duration = TimeSpan.FromSeconds(double.Parse(ffprobeResult.Format.Duration ?? "0")),
                Format = ffprobeResult.Format.FormatName ?? string.Empty,
                FileSizeBytes = new FileInfo(filePath).Length,
                Metadata = ffprobeResult.Format.Tags ?? new Dictionary<string, string>(),
                Chapters = ffprobeResult.Chapters?.Select(c => new ChapterInfo
                {
                    Index = c.Id,
                    StartTime = TimeSpan.FromSeconds(double.Parse(c.StartTime ?? "0")),
                    EndTime = TimeSpan.FromSeconds(double.Parse(c.EndTime ?? "0")),
                    Title = c.Tags?.Title ?? string.Empty
                }).ToList() ?? new List<ChapterInfo>()
            };

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

            var metadataArgs = string.Join(" ", metadata.Select(kvp => $"-metadata \"{kvp.Key}={kvp.Value}\""));
            var arguments = $"-i \"{filePath}\" {metadataArgs} -c copy \"{outputPath}\"";
            var result = await ExecuteFFmpegProcess("ffmpeg", arguments);

            if (result.Success)
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
                Message = "Failed to update metadata",
                ErrorDetails = result.Error
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
                    var arguments = $"-i \"{filePath}\" -ss {chapter.StartTime} -to {chapter.EndTime} -c copy \"{outputPath}\"";
                    var result = await ExecuteFFmpegProcess("ffmpeg", arguments);

                    if (result.Success)
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
                    var arguments = $"-i \"{filePath}\" -ss {startTime} -t {duration} -c copy \"{outputPath}\"";
                    var result = await ExecuteFFmpegProcess("ffmpeg", arguments);

                    if (result.Success)
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

    public async Task<OperationStartResult> SplitFileAsyncWithProgress(string filePath, SplitOptions options, IProgressReporter? progressReporter = null)
    {
        try
        {
            _logger.LogInformation("Starting split operation with progress tracking for: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileInfo = await GetFileInfoAsync(filePath);
            if (fileInfo == null)
            {
                throw new InvalidOperationException("Could not analyze file");
            }

            var totalSteps = 0;
            var operationType = string.Empty;

            if (options.SplitByChapters && fileInfo.Chapters.Any())
            {
                totalSteps = fileInfo.Chapters.Count;
                operationType = "SplitFileByChapters";
            }
            else if (options.MaxDuration.HasValue)
            {
                var maxDuration = options.MaxDuration.Value;
                var totalDuration = fileInfo.Duration;
                totalSteps = (int)Math.Ceiling(totalDuration.TotalSeconds / maxDuration.TotalSeconds);
                operationType = "SplitFileByDuration";
            }

            if (totalSteps == 0)
            {
                throw new InvalidOperationException("No split operation configured - specify either MaxDuration or SplitByChapters");
            }

            if (progressReporter is OperationTrackingService trackingService)
            {
                var operationKey = new OperationKey
                {
                    FilePath = filePath,
                    OperationType = operationType,
                    Options = options
                };

                var startResult = trackingService.StartOperationWithDeduplication(operationKey, totalSteps, "Preparing to split file");

                if (!startResult.IsNewOperation)
                {
                    _logger.LogInformation("Returning existing operation {OperationId} for duplicate request", startResult.OperationId);
                    return startResult;
                }

                _ = Task.Run(async () => await ExecuteSplitOperationInBackground(filePath, options, fileInfo, startResult.OperationId, totalSteps, progressReporter));

                return new OperationStartResult
                {
                    OperationId = startResult.OperationId,
                    IsNewOperation = true,
                    Message = $"Split operation started for {totalSteps} parts. Use operation ID to monitor progress."
                };
            }
            else if (progressReporter != null)
            {
                // Fallback for any non-null IProgressReporter (e.g., DummyProgressReporter in tests)
                var operationId = Guid.NewGuid().ToString();
                _ = Task.Run(async () => await ExecuteSplitOperationInBackground(filePath, options, fileInfo, operationId, totalSteps, progressReporter));
                return new OperationStartResult
                {
                    OperationId = operationId,
                    IsNewOperation = true,
                    Message = $"Split operation started for {totalSteps} parts. Use operation ID to monitor progress."
                };
            }

            throw new InvalidOperationException("Progress reporter is required for async operations");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting split operation with progress tracking for: {FilePath}", filePath);
            throw;
        }
    }

    private async Task ExecuteSplitOperationInBackground(string filePath, SplitOptions options, AudioFileInfo fileInfo, string operationId, int totalSteps, IProgressReporter? progressReporter)
    {
        var outputFiles = new List<string>();

        try
        {
            if (options.SplitByChapters && fileInfo.Chapters.Any())
            {
                for (int i = 0; i < fileInfo.Chapters.Count; i++)
                {
                    var chapter = fileInfo.Chapters[i];
                    var chapterTitle = string.IsNullOrEmpty(chapter.Title) ? $"Chapter {i + 1}" : chapter.Title;

                    await progressReporter?.ReportProgressAsync(new OperationProgressUpdate
                    {
                        OperationId = operationId,
                        CurrentStep = i + 1,
                        TotalSteps = totalSteps,
                        CurrentOperation = $"Processing {chapterTitle}"
                    })!;

                    var outputPath = GenerateChapterOutputPath(filePath, i + 1, chapter.Title, options.OutputPattern);
                    var arguments = $"-i \"{filePath}\" -ss {chapter.StartTime} -to {chapter.EndTime} -c copy \"{outputPath}\"";
                    var result = await ExecuteFFmpegProcess("ffmpeg", arguments);

                    if (result.Success)
                    {
                        outputFiles.Add(outputPath);
                        _logger.LogInformation("Successfully created chapter file: {OutputPath}", outputPath);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create chapter file: {OutputPath}", outputPath);
                    }
                }
            }
            else if (options.MaxDuration.HasValue)
            {
                var maxDuration = options.MaxDuration.Value;
                var totalDuration = fileInfo.Duration;
                var segmentCount = (int)Math.Ceiling(totalDuration.TotalSeconds / maxDuration.TotalSeconds);

                for (int i = 0; i < segmentCount; i++)
                {
                    await progressReporter?.ReportProgressAsync(new OperationProgressUpdate
                    {
                        OperationId = operationId,
                        CurrentStep = i + 1,
                        TotalSteps = totalSteps,
                        CurrentOperation = $"Processing segment {i + 1} of {segmentCount}"
                    })!;

                    var startTime = TimeSpan.FromSeconds(i * maxDuration.TotalSeconds);
                    var duration = i == segmentCount - 1
                        ? totalDuration - startTime
                        : maxDuration;

                    var outputPath = GenerateSegmentOutputPath(filePath, i + 1, options.OutputPattern);
                    var arguments = $"-i \"{filePath}\" -ss {startTime} -t {duration} -c copy \"{outputPath}\"";
                    var result = await ExecuteFFmpegProcess("ffmpeg", arguments);

                    if (result.Success)
                    {
                        outputFiles.Add(outputPath);
                        _logger.LogInformation("Successfully created segment file: {OutputPath}", outputPath);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create segment file: {OutputPath}", outputPath);
                    }
                }
            }

            var operationResult = new OperationResult
            {
                Success = outputFiles.Any(),
                Message = outputFiles.Any() ? $"File split into {outputFiles.Count} parts" : "No files were created",
                OutputFiles = outputFiles
            };

            await progressReporter?.ReportCompletionAsync(operationId, operationResult)!;
            _logger.LogInformation("Split operation completed for: {FilePath}, created {Count} files", filePath, outputFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in background split operation for: {FilePath}", filePath);
            await progressReporter?.ReportErrorAsync(operationId, ex.Message)!;
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

            var argsBuilder = new System.Text.StringBuilder();
            argsBuilder.Append($"-i \"{filePath}\" ");

            if (!string.IsNullOrEmpty(options.OutputFormat))
            {
                argsBuilder.Append($"-f {options.OutputFormat} ");
            }
            if (options.Bitrate.HasValue)
            {
                argsBuilder.Append($"-b:a {options.Bitrate.Value}k ");
            }
            if (!string.IsNullOrEmpty(options.Codec))
            {
                argsBuilder.Append($"-c:a {options.Codec} ");
            }
            foreach (var customOption in options.CustomOptions)
            {
                argsBuilder.Append($"{customOption.Key} {customOption.Value} ");
            }

            argsBuilder.Append($"\"{outputPath}\"");

            var result = await ExecuteFFmpegProcess("ffmpeg", argsBuilder.ToString());

            if (result.Success)
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
                Message = "Failed to convert file",
                ErrorDetails = result.Error
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
                var arguments = $"-i \"{filePath}\" -i \"{chapterFilePath}\" -map_metadata 1 -map_chapters 1 -c copy \"{outputPath}\"";
                var result = await ExecuteFFmpegProcess("ffmpeg", arguments);

                if (result.Success)
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
                    Message = "Failed to set chapters",
                    ErrorDetails = result.Error
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