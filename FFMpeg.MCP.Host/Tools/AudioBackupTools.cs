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
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace FFMpeg.MCP.Host.Tools;

// Response models have been moved to FFMpeg.MCP.Host.Models.Output

[McpServerToolType]
public class AudioBackupTools
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<AudioBackupTools> _logger;
    private readonly McpDispatcher _dispatcher;

    public AudioBackupTools(IFFmpegService ffmpegService, ILogger<AudioBackupTools> logger, McpDispatcher dispatcher)
    {
        _ffmpegService = ffmpegService;
        _logger = logger;
        _dispatcher = dispatcher;
    }

    [McpServerTool, Description("Create a backup copy of an audio file")]
    public Task<McpResponse<CreateAudioBackupResponse>> CreateAudioBackupAsync(
        [Description("Full path to the audio file to backup")] string filePath,
        [Description("Custom backup location (optional - will generate if not provided)")] string? backupPath = null)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is required", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Audio file not found", filePath);

            var result = await _ffmpegService.BackupFileAsync(filePath, backupPath);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Backup failed: {result.Message} {result.ErrorDetails}");
            }

            var originalInfo = await _ffmpegService.GetFileInfoAsync(filePath);

            return new CreateAudioBackupResponse
            {
                Message = result.Message,
                OriginalFile = filePath,
                BackupFile = result.OutputFiles.First(),
                FileInfo = new AudioFileInfoResponse
                {
                    FileName = originalInfo?.FileName,
                    FileSizeBytes = originalInfo?.FileSizeBytes ?? 0,
                    Duration = originalInfo?.Duration.ToString(@"hh\:mm\:ss"),
                    Format = originalInfo?.Format
                },
                BackupCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        });
    }

    [McpServerTool, Description("Create backups of multiple audio files")]
    public Task<McpResponse<CreateBatchBackupResponse>> CreateBatchBackupAsync(
        [Description("JSON array of file paths to backup")] string filePathsJson,
        [Description("Backup directory (optional - will use source directories if not provided)")] string? backupDirectory = null)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            var filePaths = JsonSerializer.Deserialize<string[]>(filePathsJson);
            if (filePaths == null || !filePaths.Any())
            {
                throw new ArgumentException("Invalid or empty file paths JSON provided", nameof(filePathsJson));
            }

            var results = new List<BatchBackupResult>();
            var successCount = 0;
            var failureCount = 0;

            foreach (var filePath in filePaths)
            {
                try
                {
                    string? customBackupPath = null;
                    if (!string.IsNullOrEmpty(backupDirectory))
                    {
                        if (!Directory.Exists(backupDirectory))
                        {
                            Directory.CreateDirectory(backupDirectory);
                        }

                        var fileName = Path.GetFileName(filePath);
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        customBackupPath = Path.Combine(backupDirectory, $"{fileName}.backup_{timestamp}");
                    }

                    var result = await _ffmpegService.BackupFileAsync(filePath, customBackupPath);

                    results.Add(new BatchBackupResult
                    {
                        OriginalFile = filePath,
                        BackupFile = result.Success ? result.OutputFiles.FirstOrDefault() : null,
                        Success = result.Success,
                        Message = result.Message
                    });

                    if (result.Success)
                        successCount++;
                    else
                        failureCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to backup file {FilePath} in batch operation.", filePath);
                    results.Add(new BatchBackupResult
                    {
                        OriginalFile = filePath,
                        Success = false,
                        Message = $"Error: {ex.Message}"
                    });
                    failureCount++;
                }
            }

            return new CreateBatchBackupResponse
            {
                Message = $"Batch backup completed: {successCount} successful, {failureCount} failed",
                TotalFiles = filePaths.Length,
                SuccessCount = successCount,
                FailureCount = failureCount,
                BackupDirectory = backupDirectory,
                BackupCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Results = results
            };
        });
    }

    [McpServerTool, Description("Create a compressed backup archive of audio files")]
    public Task<McpResponse<CreateArchiveBackupResponse>> CreateArchiveBackupAsync(
        [Description("JSON array of file paths to include in archive")] string filePathsJson,
        [Description("Output archive path (ZIP file)")] string archivePath)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            var filePaths = JsonSerializer.Deserialize<string[]>(filePathsJson);
            if (filePaths == null || !filePaths.Any())
            {
                throw new ArgumentException("Invalid or empty file paths JSON provided", nameof(filePathsJson));
            }
            if (string.IsNullOrWhiteSpace(archivePath))
            {
                throw new ArgumentException("Archive path is required", nameof(archivePath));
            }

            var archiveDirectory = Path.GetDirectoryName(archivePath);
            if (!string.IsNullOrEmpty(archiveDirectory) && !Directory.Exists(archiveDirectory))
            {
                Directory.CreateDirectory(archiveDirectory);
            }

            var metadataInfo = new List<object>();

            using (var zip = new ZipArchive(File.Create(archivePath), ZipArchiveMode.Create))
            {
                foreach (var filePath in filePaths)
                {
                    if (!File.Exists(filePath))
                    {
                        _logger.LogWarning("File not found during archive creation: {FilePath}", filePath);
                        continue; // Or throw? For now, we skip as per original logic.
                    }

                    var fileName = Path.GetFileName(filePath);
                    var entry = zip.CreateEntry(fileName);

                    using (var entryStream = entry.Open())
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        await fileStream.CopyToAsync(entryStream);
                    }
                }
            }
            var archiveInfo = new FileInfo(archivePath);
            return new CreateArchiveBackupResponse
            {
                Message = "Archive backup created successfully",
                ArchivePath = archivePath,
                ArchiveSize = archiveInfo.Length,
                FilesIncluded = filePaths.Length,
                ArchiveCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        });
    }

    [McpServerTool, Description("Restore an audio file from backup")]
    public Task<McpResponse<RestoreFromBackupResponse>> RestoreFromBackupAsync(
        [Description("Full path to the backup file")] string backupPath,
        [Description("Target restore location (optional - will use original location if available)")] string? restorePath = null)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException("Backup file not found", backupPath);
            }

            if (string.IsNullOrEmpty(restorePath))
            {
                var backupFileName = Path.GetFileName(backupPath);
                if (backupFileName.Contains(".backup_"))
                {
                    var originalFileName = backupFileName.Substring(0, backupFileName.IndexOf(".backup_"));
                    var backupDirectory = Path.GetDirectoryName(backupPath);
                    restorePath = Path.Combine(backupDirectory ?? string.Empty, originalFileName);
                }
                else
                {
                    throw new InvalidOperationException("Cannot determine restore location. Please specify restorePath parameter.");
                }
            }

            var restoreDirectory = Path.GetDirectoryName(restorePath);
            if (!string.IsNullOrEmpty(restoreDirectory) && !Directory.Exists(restoreDirectory))
            {
                Directory.CreateDirectory(restoreDirectory);
            }

            await using var sourceStream = File.OpenRead(backupPath);
            await using var destinationStream = File.Create(restorePath);
            await sourceStream.CopyToAsync(destinationStream);

            var restoredInfo = await _ffmpegService.GetFileInfoAsync(restorePath);

            return new RestoreFromBackupResponse
            {
                Message = "File restored from backup successfully",
                BackupFile = backupPath,
                RestoredFile = restorePath,
                FileInfo = restoredInfo != null ? new AudioFileInfoResponse
                {
                    FileName = restoredInfo.FileName,
                    FileSizeBytes = restoredInfo.FileSizeBytes,
                    Duration = restoredInfo.Duration.ToString(@"hh\:mm\:ss"),
                    Format = restoredInfo.Format
                } : null,
                RestoredAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        });
    }

    [McpServerTool, Description("List all backup files in a directory")]
    public Task<McpResponse<ListBackupsResponse>> ListBackupsAsync(
        [Description("Directory to search for backup files")] string directory,
        [Description("Include subdirectories in search")] bool includeSubdirectories = false)
    {
        return _dispatcher.DispatchAsync(() =>
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directory}");
            }

            var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var backupFiles = Directory.GetFiles(directory, "*.backup_*", searchOption);

            var backupInfoList = new List<BackupInfo>();

            foreach (var backupFile in backupFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(backupFile);
                    var fileName = Path.GetFileName(backupFile);
                    string? originalFileName = null;
                    string? backupTimestamp = null;

                    if (fileName.Contains(".backup_"))
                    {
                        var parts = fileName.Split(new[] { ".backup_" }, StringSplitOptions.None);
                        if (parts.Length == 2)
                        {
                            originalFileName = parts[0];
                            backupTimestamp = parts[1];
                        }
                    }

                    backupInfoList.Add(new BackupInfo
                    {
                        BackupFile = backupFile,
                        OriginalFileName = originalFileName,
                        BackupTimestamp = backupTimestamp,
                        FileSize = fileInfo.Length,
                        Created = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        Modified = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading backup file info: {BackupFile}", backupFile);
                }
            }

            return Task.FromResult(new ListBackupsResponse
            {
                Message = $"Found {backupFiles.Length} backup files",
                Directory = directory,
                IncludeSubdirectories = includeSubdirectories,
                BackupCount = backupFiles.Length,
                Backups = backupInfoList.OrderByDescending(b => b.Created).ToList()
            });
        });
    }

    [McpServerTool, Description("Clean up old backup files based on age or count")]
    public Task<McpResponse<CleanupBackupsResponse>> CleanupBackupsAsync(
        [Description("Directory containing backup files")] string directory,
        [Description("Maximum age of backups to keep in days")] int? maxAgeDays = null,
        [Description("Maximum number of backups to keep per original file")] int? maxBackupsPerFile = null,
        [Description("Perform dry run (don't actually delete files)")] bool dryRun = true)
    {
        return _dispatcher.DispatchAsync(() =>
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directory}");
            }

            var backupFiles = Directory.GetFiles(directory, "*.backup_*", SearchOption.TopDirectoryOnly)
                .Select(f => new FileInfo(f))
                .ToList();

            var filesToDelete = new List<FileInfo>();

            if (maxBackupsPerFile.HasValue)
            {
                var groupedFiles = backupFiles
                    .GroupBy(f => ExtractOriginalFileName(f.Name))
                    .Where(g => !string.IsNullOrEmpty(g.Key));

                foreach (var group in groupedFiles)
                {
                    var sortedFiles = group.OrderByDescending(f => f.CreationTime).ToList();
                    if (sortedFiles.Count > maxBackupsPerFile.Value)
                    {
                        filesToDelete.AddRange(sortedFiles.Skip(maxBackupsPerFile.Value));
                    }
                }
            }

            if (maxAgeDays.HasValue)
            {
                var cutoffDate = DateTime.Now.AddDays(-maxAgeDays.Value);
                var oldFiles = backupFiles.Where(f => f.CreationTime < cutoffDate);
                filesToDelete.AddRange(oldFiles);
            }

            filesToDelete = filesToDelete.DistinctBy(f => f.FullName).ToList();

            var deletedFiles = new List<string>();
            var deletedSize = 0L;

            if (!dryRun)
            {
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        deletedSize += file.Length;
                        file.Delete();
                        deletedFiles.Add(file.FullName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete backup file: {FilePath}", file.FullName);
                    }
                }
            }

            return Task.FromResult(new CleanupBackupsResponse
            {
                Message = dryRun ? $"Dry run: Would delete {filesToDelete.Count} backup files" : $"Deleted {deletedFiles.Count} backup files",
                Directory = directory,
                DryRun = dryRun,
                MaxAgeDays = maxAgeDays,
                MaxBackupsPerFile = maxBackupsPerFile,
                TotalBackupFiles = backupFiles.Count,
                FilesToDelete = filesToDelete.Count,
                DeletedFiles = deletedFiles,
                SpaceSavedBytes = deletedSize,
                SpaceSavedMB = Math.Round(deletedSize / (1024.0 * 1024.0), 2)
            });
        });
    }

    private static string? ExtractOriginalFileName(string backupFileName)
    {
        if (backupFileName.Contains(".backup_"))
        {
            return backupFileName.Substring(0, backupFileName.IndexOf(".backup_"));
        }
        return null;
    }
}