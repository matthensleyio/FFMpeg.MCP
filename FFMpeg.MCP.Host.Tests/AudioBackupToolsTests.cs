using FFMpeg.MCP.Host.Mcp;
using FFMpeg.MCP.Host.Tools;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace FFMpeg.MCP.Host.Tests;

public class AudioBackupToolsTests : TestBase
{
    private readonly AudioBackupTools _backupTools;

    public AudioBackupToolsTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AudioBackupTools>();
        var dispatcherLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<McpDispatcher>();
        var dispatcher = new McpDispatcher(dispatcherLogger);
        _backupTools = new AudioBackupTools(FFmpegService, logger, dispatcher);
    }

    [Fact]
    public async Task CreateAudioBackupAsync_WithValidFile_CreatesBackup()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");

        // Act
        var response = await _backupTools.CreateAudioBackupAsync(inputFile);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);

        var result = response.Result;
        Assert.NotNull(result.BackupFile);
        TrackCreatedFile(result.BackupFile);
        AssertFileExists(result.BackupFile);

        var originalSize = GetFileSize(inputFile);
        var backupSize = GetFileSize(result.BackupFile);
        Assert.Equal(originalSize, backupSize);

        Assert.Contains(".backup_", result.BackupFile);
    }

    [Fact]
    public async Task CreateAudioBackupAsync_WithCustomPath_CreatesBackupAtPath()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");
        var customBackupPath = GetWorkingPath("custom-backup.mp3");

        // Act
        var response = await _backupTools.CreateAudioBackupAsync(inputFile, customBackupPath);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);

        var result = response.Result;
        Assert.Equal(customBackupPath, result.BackupFile);
        AssertFileExists(customBackupPath);

        var originalSize = GetFileSize(inputFile);
        var backupSize = GetFileSize(customBackupPath);
        Assert.Equal(originalSize, backupSize);
    }

    [Fact]
    public async Task CreateBatchBackupAsync_WithMultipleFiles_CreatesAllBackups()
    {
        // Arrange
        var inputFile1 = CopyTestFile("sample-short.mp3");
        var inputFile2 = CopyTestFile("sample-short.wav");
        var filePaths = System.Text.Json.JsonSerializer.Serialize(new[] { inputFile1, inputFile2 });

        // Act
        var response = await _backupTools.CreateBatchBackupAsync(filePaths);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);

        var result = response.Result;
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);

        Assert.NotNull(result.Results);
        foreach (var fileResult in result.Results)
        {
            if (fileResult.Success)
            {
                Assert.NotNull(fileResult.BackupFile);
                TrackCreatedFile(fileResult.BackupFile);
                AssertFileExists(fileResult.BackupFile);
            }
        }
    }

    [Fact]
    public async Task CreateArchiveBackupAsync_WithMultipleFiles_CreatesZipArchive()
    {
        // Arrange
        var inputFile1 = CopyTestFile("sample-short.mp3");
        var inputFile2 = CopyTestFile("sample-short.wav");
        var filePaths = System.Text.Json.JsonSerializer.Serialize(new[] { inputFile1, inputFile2 });
        var archivePath = GetWorkingPath("backup-archive.zip");

        // Act
        var response = await _backupTools.CreateArchiveBackupAsync(filePaths, archivePath);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);

        var result = response.Result;
        AssertFileExists(archivePath);
        Assert.Equal(2, result.FilesIncluded);
        Assert.True(result.ArchiveSize > 0);

        using (var archive = ZipFile.OpenRead(archivePath))
        {
            Assert.Equal(2, archive.Entries.Count);
            Assert.Contains(archive.Entries, e => e.Name.EndsWith(".mp3"));
            Assert.Contains(archive.Entries, e => e.Name.EndsWith(".wav"));
        }
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithValidBackup_RestoresFile()
    {
        // Arrange
        var originalFile = CopyTestFile("sample-short.mp3");
        var backupResponse = await _backupTools.CreateAudioBackupAsync(originalFile);
        Assert.NotNull(backupResponse.Result?.BackupFile);
        var backupFile = backupResponse.Result.BackupFile;
        TrackCreatedFile(backupFile);
        var restorePath = GetWorkingPath("restored-file.mp3");

        // Act
        var response = await _backupTools.RestoreFromBackupAsync(backupFile, restorePath);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);

        var result = response.Result;
        Assert.Equal(restorePath, result.RestoredFile);
        AssertFileExists(restorePath);

        var originalSize = GetFileSize(originalFile);
        var restoredSize = GetFileSize(restorePath);
        Assert.Equal(originalSize, restoredSize);
    }

    [Fact]
    public async Task ListBackupsAsync_WithBackupsInDirectory_ListsBackups()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");
        await _backupTools.CreateAudioBackupAsync(inputFile);

        // Act
        var response = await _backupTools.ListBackupsAsync(WorkingDirectory, includeSubdirectories: false);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);

        var result = response.Result;
        Assert.Equal(WorkingDirectory, result.Directory);
        Assert.False(result.IncludeSubdirectories);
        Assert.True(result.BackupCount > 0);
        Assert.NotEmpty(result.Backups);
    }

    [Fact]
    public async Task CleanupBackupsAsync_WithDryRun_ReportsWhatWouldBeDeleted()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");
        var backupResponse = await _backupTools.CreateAudioBackupAsync(inputFile);
        Assert.NotNull(backupResponse.Result?.BackupFile);
        var backupFile = backupResponse.Result.BackupFile;
        TrackCreatedFile(backupFile);

        // Act
        var response = await _backupTools.CleanupBackupsAsync(WorkingDirectory, maxAgeDays: 0, dryRun: true);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);

        var result = response.Result;
        Assert.True(result.DryRun);
        Assert.Contains("Would delete", result.Message);

        AssertFileExists(backupFile);
    }

    [Fact]
    public async Task CreateAudioBackupAsync_WithNonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentFile = GetWorkingPath("nonexistent.mp3");

        // Act
        var response = await _backupTools.CreateAudioBackupAsync(nonExistentFile);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal("not_found", response.Error.Code);
        Assert.Contains("not found", response.Error.Message);
    }

    [Fact]
    public async Task CreateBatchBackupAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var response = await _backupTools.CreateBatchBackupAsync(invalidJson);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal("internal_error", response.Error.Code); // JSON deserialization error
    }

    [Fact]
    public async Task CreateArchiveBackupAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var archivePath = GetWorkingPath("test.zip");

        // Act
        var response = await _backupTools.CreateArchiveBackupAsync(invalidJson, archivePath);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal("internal_error", response.Error.Code); // JSON deserialization error
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithNonExistentBackup_ReturnsError()
    {
        // Arrange
        var nonExistentBackup = GetWorkingPath("nonexistent.backup");
        var restorePath = GetWorkingPath("restored.mp3");

        // Act
        var response = await _backupTools.RestoreFromBackupAsync(nonExistentBackup, restorePath);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal("not_found", response.Error.Code);
        Assert.Contains("not found", response.Error.Message);
    }

    [Fact]
    public async Task ListBackupsAsync_WithNonExistentDirectory_ReturnsError()
    {
        // Arrange
        var nonExistentDir = GetWorkingPath("nonexistent-dir");

        // Act
        var response = await _backupTools.ListBackupsAsync(nonExistentDir);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        Assert.Equal("not_found", response.Error.Code);
        Assert.Contains("not found", response.Error.Message);
    }
}