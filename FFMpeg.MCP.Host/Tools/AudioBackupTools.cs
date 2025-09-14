using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using FFMpeg.MCP.Host.Models;
using FFMpeg.MCP.Host.Services;
using System.ComponentModel;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tools;

[McpServerToolType]
public class AudioBackupTools
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<AudioBackupTools> _logger;

    public AudioBackupTools(IFFmpegService ffmpegService, ILogger<AudioBackupTools> logger)
    {
        _ffmpegService = ffmpegService;
        _logger = logger;
    }

    [McpServerTool, Description("Create a backup copy of an audio file")]
    public async Task<string> CreateAudioBackupAsync(
        [Description("Full path to the audio file to backup")] string filePath,
        [Description("Custom backup location (optional - will generate if not provided)")] string? backupPath = null)
    {
        try
        {
            var result = await _ffmpegService.BackupFileAsync(filePath, backupPath);

            if (result.Success)
            {
                var originalInfo = await _ffmpegService.GetFileInfoAsync(filePath);
                var backupInfo = await _ffmpegService.GetFileInfoAsync(result.OutputFiles.First());

                var response = new
                {
                    success = true,
                    message = result.Message,
                    originalFile = filePath,
                    backupFile = result.OutputFiles.First(),
                    fileInfo = new
                    {
                        fileName = originalInfo?.FileName,
                        fileSize = originalInfo?.FileSizeBytes,
                        duration = originalInfo?.Duration.ToString(@"hh\:mm\:ss"),
                        format = originalInfo?.Format
                    },
                    backupCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return JsonSerializer.Serialize(new { success = false, message = result.Message, errorDetails = result.ErrorDetails }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup for {FilePath}", filePath);
            return JsonSerializer.Serialize(new { success = false, message = $"Error creating backup: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Create backups of multiple audio files")]
    public async Task<string> CreateBatchBackupAsync(
        [Description("JSON array of file paths to backup")] string filePathsJson,
        [Description("Backup directory (optional - will use source directories if not provided)")] string? backupDirectory = null)
    {
        try
        {
            var filePaths = JsonSerializer.Deserialize<string[]>(filePathsJson);
            if (filePaths == null || !filePaths.Any())
            {
                throw new InvalidOperationException("Invalid or empty file paths JSON provided");
            }

            var results = new List<object>();
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

                    results.Add(new
                    {
                        originalFile = filePath,
                        backupFile = result.Success ? result.OutputFiles.FirstOrDefault() : null,
                        success = result.Success,
                        message = result.Message
                    });

                    if (result.Success)
                        successCount++;
                    else
                        failureCount++;
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        originalFile = filePath,
                        success = false,
                        message = $"Error: {ex.Message}"
                    });
                    failureCount++;
                }
            }

            var response = new
            {
                success = successCount > 0,
                message = $"Batch backup completed: {successCount} successful, {failureCount} failed",
                totalFiles = filePaths.Length,
                successCount = successCount,
                failureCount = failureCount,
                backupDirectory = backupDirectory,
                backupCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                results = results
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch backup operation");
            return JsonSerializer.Serialize(new { success = false, message = $"Error in batch backup operation: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Create a compressed backup archive of audio files")]
    public async Task<string> CreateArchiveBackupAsync(
        [Description("JSON array of file paths to include in archive")] string filePathsJson,
        [Description("Output archive path (ZIP file)")] string archivePath)
    {
        try
        {
            var filePaths = JsonSerializer.Deserialize<string[]>(filePathsJson);
            if (filePaths == null || !filePaths.Any())
            {
                throw new InvalidOperationException("Invalid or empty file paths JSON provided");
            }

            var archiveDirectory = Path.GetDirectoryName(archivePath);
            if (!string.IsNullOrEmpty(archiveDirectory) && !Directory.Exists(archiveDirectory))
            {
                Directory.CreateDirectory(archiveDirectory);
            }

            var metadataInfo = new List<object>();

            using (var zip = new System.IO.Compression.ZipArchive(
                File.Create(archivePath),
                System.IO.Compression.ZipArchiveMode.Create))
            {
                foreach (var filePath in filePaths)
                {
                    if (!File.Exists(filePath))
                    {
                        _logger.LogError("File not found during archive creation: {FilePath}", filePath);
                        continue;
                    }

                    // Add the audio file to the archive
                    var fileName = Path.GetFileName(filePath);
                    var entry = zip.CreateEntry(fileName);

                    using (var entryStream = entry.Open())
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        await fileStream.CopyToAsync(entryStream);
                    }

                    var fileInfo = await _ffmpegService.GetFileInfoAsync(filePath);
                    if (fileInfo != null)
                    {
                        metadataInfo.Add(new
                        {
                            fileName = fileName,
                            originalPath = filePath,
                            fileSize = fileInfo.FileSizeBytes,
                            duration = fileInfo.Duration.ToString(@"hh\:mm\:ss"),
                            format = fileInfo.Format,
                            metadata = fileInfo.Metadata,
                            chapters = fileInfo.Chapters.Select(c => new
                            {
                                index = c.Index,
                                title = c.Title,
                                startTime = c.StartTime.ToString(@"hh\:mm\:ss"),
                                endTime = c.EndTime.ToString(@"hh\:mm\:ss")
                            }).ToList()
                        });
                    }
                }


                // Add metadata file to archive if requested
                if (metadataInfo.Any())
                {
                    var metadataEntry = zip.CreateEntry("metadata.json");
                    using (var metadataStream = metadataEntry.Open())
                    using (var writer = new StreamWriter(metadataStream))
                    {
                        var metadataJson = JsonSerializer.Serialize(new
                        {
                            archiveCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            totalFiles = metadataInfo.Count,
                            files = metadataInfo
                        }, new JsonSerializerOptions { WriteIndented = true });

                        await writer.WriteAsync(metadataJson);
                    }
                }
            }

            var archiveInfo = new FileInfo(archivePath);
            var response = new
            {
                success = true,
                message = "Archive backup created successfully",
                archivePath = archivePath,
                archiveSize = archiveInfo.Length,
                filesIncluded = metadataInfo.Count,
                archiveCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating archive backup to {ArchivePath}", archivePath);
            return JsonSerializer.Serialize(new { success = false, message = $"Error creating archive backup: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Restore an audio file from backup")]
    public async Task<string> RestoreFromBackupAsync(
        [Description("Full path to the backup file")] string backupPath,
        [Description("Target restore location (optional - will use original location if available)")] string? restorePath = null)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    message = $"Backup file not found: {backupPath}"
                });
            }

            // If no restore path specified, try to derive from backup filename
            if (string.IsNullOrEmpty(restorePath))
            {
                // Try to extract original filename from backup naming convention
                var backupFileName = Path.GetFileName(backupPath);
                if (backupFileName.Contains(".backup_"))
                {
                    var originalFileName = backupFileName.Substring(0, backupFileName.IndexOf(".backup_"));
                    var backupDirectory = Path.GetDirectoryName(backupPath);
                    restorePath = Path.Combine(backupDirectory ?? string.Empty, originalFileName);
                }
                else
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "Cannot determine restore location. Please specify restorePath parameter."
                    });
                }
            }

            // Create directory if it doesn't exist
            var restoreDirectory = Path.GetDirectoryName(restorePath);
            if (!string.IsNullOrEmpty(restoreDirectory) && !Directory.Exists(restoreDirectory))
            {
                Directory.CreateDirectory(restoreDirectory);
            }

            // Copy backup file to restore location
            await using var sourceStream = File.OpenRead(backupPath);
            await using var destinationStream = File.Create(restorePath);
            await sourceStream.CopyToAsync(destinationStream);

            // Verify the restored file
            var restoredInfo = await _ffmpegService.GetFileInfoAsync(restorePath);

            var response = new
            {
                success = true,
                message = "File restored from backup successfully",
                backupFile = backupPath,
                restoredFile = restorePath,
                fileInfo = restoredInfo != null ? new
                {
                    fileName = restoredInfo.FileName,
                    fileSize = restoredInfo.FileSizeBytes,
                    duration = restoredInfo.Duration.ToString(@"hh\:mm\:ss"),
                    format = restoredInfo.Format
                } : null,
                restoredAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring from backup {BackupPath}", backupPath);
            return JsonSerializer.Serialize(new { success = false, message = $"Error restoring from backup: {ex.Message}" });
        }
    }

    [McpServerTool, Description("List all backup files in a directory")]
    public Task<string> ListBackupsAsync(
        [Description("Directory to search for backup files")] string directory,
        [Description("Include subdirectories in search")] bool includeSubdirectories = false)
    {
        try
        {
            if (!Directory.Exists(directory))
            {
                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = false,
                    message = $"Directory not found: {directory}"
                }));
            }

            var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var backupFiles = Directory.GetFiles(directory, "*.backup_*", searchOption);

            var backupInfoList = new List<object>();

            foreach (var backupFile in backupFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(backupFile);
                    var fileName = Path.GetFileName(backupFile);

                    // Try to extract original filename and timestamp from backup naming convention
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

                    backupInfoList.Add(new
                    {
                        backupFile = backupFile,
                        originalFileName = originalFileName,
                        backupTimestamp = backupTimestamp,
                        fileSize = fileInfo.Length,
                        created = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        modified = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading backup file info: {BackupFile}", backupFile);
                }
            }

            var response = new
            {
                success = true,
                message = $"Found {backupFiles.Length} backup files",
                directory = directory,
                includeSubdirectories = includeSubdirectories,
                backupCount = backupFiles.Length,
                backups = backupInfoList.OrderByDescending(b => ((dynamic)b).created).ToList()
            };

            return Task.FromResult(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backups in {Directory}", directory);
            return Task.FromResult(JsonSerializer.Serialize(new { success = false, message = $"Error listing backups: {ex.Message}" }));
        }
    }

    [McpServerTool, Description("Clean up old backup files based on age or count")]
    public Task<string> CleanupBackupsAsync(
        [Description("Directory containing backup files")] string directory,
        [Description("Maximum age of backups to keep in days")] int? maxAgeDays = null,
        [Description("Maximum number of backups to keep per original file")] int? maxBackupsPerFile = null,
        [Description("Perform dry run (don't actually delete files)")] bool dryRun = true)
    {
        try
        {
            if (!Directory.Exists(directory))
            {
                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = false,
                    message = $"Directory not found: {directory}"
                }));
            }

            var backupFiles = Directory.GetFiles(directory, "*.backup_*", SearchOption.TopDirectoryOnly)
                .Select(f => new FileInfo(f))
                .ToList();

            var filesToDelete = new List<FileInfo>();

            // Group by original filename if maxBackupsPerFile is specified
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

            // Filter by age if maxAgeDays is specified
            if (maxAgeDays.HasValue)
            {
                var cutoffDate = DateTime.Now.AddDays(-maxAgeDays.Value);
                var oldFiles = backupFiles.Where(f => f.CreationTime < cutoffDate);
                filesToDelete.AddRange(oldFiles);
            }

            // Remove duplicates
            filesToDelete = filesToDelete.Distinct().ToList();

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

            var response = new
            {
                success = true,
                message = dryRun ? $"Dry run: Would delete {filesToDelete.Count} backup files" : $"Deleted {deletedFiles.Count} backup files",
                directory = directory,
                dryRun = dryRun,
                maxAgeDays = maxAgeDays,
                maxBackupsPerFile = maxBackupsPerFile,
                totalBackupFiles = backupFiles.Count,
                filesToDelete = filesToDelete.Count,
                deletedFiles = deletedFiles,
                spaceSavedBytes = deletedSize,
                spaceSavedMB = Math.Round(deletedSize / (1024.0 * 1024.0), 2)
            };

            return Task.FromResult(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up backups in {Directory}", directory);
            return Task.FromResult(JsonSerializer.Serialize(new { success = false, message = $"Error cleaning up backups: {ex.Message}" }));
        }
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