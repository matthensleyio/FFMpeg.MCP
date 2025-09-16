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
using System.Text;

namespace FFMpeg.MCP.Host.Tools;

#region Request Models
public class ConcatenationRequest
{
    public List<string> InputFiles { get; set; } = new();
    public List<string>? ChapterTitles { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Narrator { get; set; }
    public string? Genre { get; set; }
    public string? Description { get; set; }
    public int? Year { get; set; }
}
#endregion

#region Response Models
public class ConcatenationResponse
{
    public string? Message { get; set; }
    public List<string>? OutputFiles { get; set; }
    public ConcatenationInfo? Concatenation { get; set; }
}

public class ConcatenationInfo
{
    public int InputFileCount { get; set; }
    public string? OutputFormat { get; set; }
    public long? TotalInputSize { get; set; }
    public long? OutputSize { get; set; }
    public string? TotalDuration { get; set; }
    public int ChapterCount { get; set; }
    public List<AudiobookChapter>? Chapters { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class AudiobookChapter
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
}
#endregion

[McpServerToolType]
public class AudiobookTools
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<AudiobookTools> _logger;
    private readonly McpDispatcher _dispatcher;

    public AudiobookTools(IFFmpegService ffmpegService, ILogger<AudiobookTools> logger, McpDispatcher dispatcher)
    {
        _ffmpegService = ffmpegService;
        _logger = logger;
        _dispatcher = dispatcher;
    }

    [McpServerTool, Description("Concatenate multiple MP3 files into a single M4B audiobook with chapters and metadata")]
    public Task<McpResponse<ConcatenationResponse>> ConcatenateToAudiobookAsync(
        [Description("JSON array of input MP3 file paths in the order they should be concatenated")] string inputFilesJson,
        [Description("Full path for the output M4B file")] string outputPath,
        [Description("Audiobook title (optional)")] string? title = null,
        [Description("Author name (optional)")] string? author = null,
        [Description("Narrator name (optional)")] string? narrator = null,
        [Description("JSON array of custom chapter titles (optional, will use filenames if not provided)")] string? chapterTitlesJson = null,
        [Description("Genre (optional)")] string? genre = null,
        [Description("Description/summary (optional)")] string? description = null,
        [Description("Publication year (optional)")] int? year = null)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(inputFilesJson)) throw new ArgumentException("Input files JSON is required.", nameof(inputFilesJson));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path is required.", nameof(outputPath));

            var inputFiles = JsonSerializer.Deserialize<string[]>(inputFilesJson);
            if (inputFiles == null || !inputFiles.Any()) throw new ArgumentException("Invalid or empty input files JSON provided", nameof(inputFilesJson));

            var chapterTitles = new List<string>();
            if (!string.IsNullOrWhiteSpace(chapterTitlesJson))
            {
                var customTitles = JsonSerializer.Deserialize<string[]>(chapterTitlesJson);
                if (customTitles != null) chapterTitles.AddRange(customTitles);
            }

            // Validate all input files exist
            foreach (var inputFile in inputFiles)
            {
                if (!File.Exists(inputFile))
                    throw new FileNotFoundException($"Input file not found: {inputFile}");
            }

            // Ensure output path has .m4b extension
            if (!outputPath.EndsWith(".m4b", StringComparison.OrdinalIgnoreCase))
            {
                outputPath = Path.ChangeExtension(outputPath, ".m4b");
            }

            // Create output directory if it doesn't exist
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            _logger.LogInformation("Starting concatenation of {FileCount} files to M4B audiobook", inputFiles.Length);

            // Get file information for each input file
            var fileInfos = new List<(string Path, AudioFileInfo? Info, TimeSpan CumulativeDuration)>();
            var cumulativeDuration = TimeSpan.Zero;
            var totalInputSize = 0L;

            foreach (var inputFile in inputFiles)
            {
                var fileInfo = await _ffmpegService.GetFileInfoAsync(inputFile);
                if (fileInfo == null)
                {
                    _logger.LogWarning("Could not get file info for: {InputFile}", inputFile);
                    continue;
                }

                fileInfos.Add((inputFile, fileInfo, cumulativeDuration));
                cumulativeDuration += fileInfo.Duration;
                totalInputSize += fileInfo.FileSizeBytes;
            }

            if (!fileInfos.Any())
            {
                throw new InvalidOperationException("No valid input files found");
            }

            // Generate chapters based on file durations
            var chapters = GenerateChapters(fileInfos, chapterTitles);

            // Create temporary files for FFmpeg processing
            var tempConcatFile = Path.GetTempFileName();
            var tempChapterFile = Path.GetTempFileName();

            try
            {
                // Create concat file for FFmpeg
                await CreateConcatFile(tempConcatFile, inputFiles);

                // Create chapter metadata file
                await CreateChapterMetadataFile(tempChapterFile, chapters, title, author, narrator, genre, description, year);

                // Build FFmpeg command for concatenation with chapters and metadata
                var arguments = BuildConcatenationCommand(tempConcatFile, tempChapterFile, outputPath, title, author, narrator, genre, description, year);

                // Execute FFmpeg concatenation
                var result = await ExecuteFFmpegConcatenation(arguments);

                if (!result.Success)
                {
                    throw new InvalidOperationException($"Failed to concatenate files to M4B: {result.Error}");
                }

                // Get output file info
                var outputFileInfo = new FileInfo(outputPath);
                var outputSize = outputFileInfo.Exists ? outputFileInfo.Length : 0;

                var metadata = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(title)) metadata["title"] = title;
                if (!string.IsNullOrEmpty(author)) metadata["artist"] = author;
                if (!string.IsNullOrEmpty(narrator)) metadata["albumartist"] = narrator;
                if (!string.IsNullOrEmpty(genre)) metadata["genre"] = genre;
                if (!string.IsNullOrEmpty(description)) metadata["comment"] = description;
                if (year.HasValue) metadata["date"] = year.Value.ToString();

                _logger.LogInformation("Successfully created M4B audiobook: {OutputPath}", outputPath);

                return new ConcatenationResponse
                {
                    Message = $"Successfully concatenated {inputFiles.Length} files into M4B audiobook",
                    OutputFiles = [outputPath],
                    Concatenation = new ConcatenationInfo
                    {
                        InputFileCount = inputFiles.Length,
                        OutputFormat = "m4b",
                        TotalInputSize = totalInputSize,
                        OutputSize = outputSize,
                        TotalDuration = cumulativeDuration.ToString(@"hh\:mm\:ss"),
                        ChapterCount = chapters.Count,
                        Chapters = chapters,
                        Metadata = metadata
                    }
                };
            }
            finally
            {
                // Clean up temporary files
                if (File.Exists(tempConcatFile)) File.Delete(tempConcatFile);
                if (File.Exists(tempChapterFile)) File.Delete(tempChapterFile);
            }
        });
    }

    private static List<AudiobookChapter> GenerateChapters(
        List<(string Path, AudioFileInfo? Info, TimeSpan CumulativeDuration)> fileInfos,
        List<string> customTitles)
    {
        var chapters = new List<AudiobookChapter>();
        var chapterIndex = 1;

        foreach (var (path, info, startTime) in fileInfos)
        {
            if (info == null) continue;

            var chapterTitle = customTitles.Count >= chapterIndex
                ? customTitles[chapterIndex - 1]
                : Path.GetFileNameWithoutExtension(path);

            var endTime = startTime + info.Duration;

            chapters.Add(new AudiobookChapter
            {
                Index = chapterIndex,
                Title = chapterTitle,
                StartTime = startTime.ToString(@"hh\:mm\:ss\.fff"),
                EndTime = endTime.ToString(@"hh\:mm\:ss\.fff"),
                Duration = info.Duration.ToString(@"hh\:mm\:ss"),
                SourceFile = Path.GetFileName(path)
            });

            chapterIndex++;
        }

        return chapters;
    }

    private static async Task CreateConcatFile(string tempFile, string[] inputFiles)
    {
        var concatContent = new StringBuilder();
        foreach (var inputFile in inputFiles)
        {
            // Escape single quotes for FFmpeg
            var escapedPath = inputFile.Replace("'", "'\\''");
            concatContent.AppendLine($"file '{escapedPath}'");
        }

        await File.WriteAllTextAsync(tempFile, concatContent.ToString());
    }

    private static async Task CreateChapterMetadataFile(string tempFile, List<AudiobookChapter> chapters,
        string? title, string? author, string? narrator, string? genre, string? description, int? year)
    {
        var content = new StringBuilder();
        content.AppendLine(";FFMETADATA1");

        // Add global metadata
        if (!string.IsNullOrEmpty(title)) content.AppendLine($"title={title}");
        if (!string.IsNullOrEmpty(author)) content.AppendLine($"artist={author}");
        if (!string.IsNullOrEmpty(narrator)) content.AppendLine($"albumartist={narrator}");
        if (!string.IsNullOrEmpty(genre)) content.AppendLine($"genre={genre}");
        if (!string.IsNullOrEmpty(description)) content.AppendLine($"comment={description}");
        if (year.HasValue) content.AppendLine($"date={year.Value}");

        content.AppendLine();

        // Add chapter information
        foreach (var chapter in chapters)
        {
            content.AppendLine("[CHAPTER]");
            content.AppendLine("TIMEBASE=1/1000");

            if (TimeSpan.TryParse(chapter.StartTime, out var start))
                content.AppendLine($"START={start.TotalMilliseconds:F0}");

            if (TimeSpan.TryParse(chapter.EndTime, out var end))
                content.AppendLine($"END={end.TotalMilliseconds:F0}");

            content.AppendLine($"title={chapter.Title}");
            content.AppendLine();
        }

        await File.WriteAllTextAsync(tempFile, content.ToString());
    }

    private static string BuildConcatenationCommand(string concatFile, string chapterFile, string outputPath,
        string? title, string? author, string? narrator, string? genre, string? description, int? year)
    {
        var args = new StringBuilder();
        args.Append($"-f concat -safe 0 -i \"{concatFile}\" ");
        args.Append($"-i \"{chapterFile}\" ");
        args.Append("-map 0:a -map_metadata 1 -map_chapters 1 ");
        args.Append("-c:a aac -b:a 128k ");
        args.Append("-movflags +faststart ");

        // Add specific metadata for M4B
        if (!string.IsNullOrEmpty(title)) args.Append($"-metadata title=\"{title}\" ");
        if (!string.IsNullOrEmpty(author)) args.Append($"-metadata artist=\"{author}\" ");
        if (!string.IsNullOrEmpty(narrator)) args.Append($"-metadata albumartist=\"{narrator}\" ");
        if (!string.IsNullOrEmpty(genre)) args.Append($"-metadata genre=\"{genre}\" ");
        if (!string.IsNullOrEmpty(description)) args.Append($"-metadata comment=\"{description}\" ");
        if (year.HasValue) args.Append($"-metadata date=\"{year.Value}\" ");

        args.Append($"\"{outputPath}\"");

        return args.ToString();
    }

    private async Task<ProcessExecutionResult> ExecuteFFmpegConcatenation(string arguments)
    {
        try
        {
            _logger.LogInformation("Executing FFmpeg concatenation with arguments: {Arguments}", arguments);

            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new System.Diagnostics.Process { StartInfo = processStartInfo };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("FFmpeg concatenation process exited with code {ExitCode}: {Error}", process.ExitCode, error);
                return new ProcessExecutionResult { Success = false, Output = output, Error = error };
            }

            return new ProcessExecutionResult { Success = true, Output = output, Error = error };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing FFmpeg concatenation");
            return new ProcessExecutionResult { Success = false, Error = ex.Message };
        }
    }
}