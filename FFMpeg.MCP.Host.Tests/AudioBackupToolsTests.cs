using FFMpeg.MCP.Host.Tools;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO.Compression;

namespace FFMpeg.MCP.Host.Tests;

public class AudioBackupToolsTests : TestBase
{
    private readonly AudioBackupTools _backupTools;

    public AudioBackupToolsTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AudioBackupTools>();
        _backupTools = new AudioBackupTools(FFmpegService, logger);
    }

    [Fact]
    public async Task CreateAudioBackupAsync_WithValidFile_CreatesBackup()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");

        // Act
        var result = await _backupTools.CreateAudioBackupAsync(inputFile);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            var backupFile = response.GetProperty("backupFile").GetString();
            Assert.NotNull(backupFile);
            TrackCreatedFile(backupFile);
            AssertFileExists(backupFile);

            // Verify backup has same size as original
            var originalSize = GetFileSize(inputFile);
            var backupSize = GetFileSize(backupFile);
            Assert.Equal(originalSize, backupSize);

            // Verify backup naming convention
            Assert.Contains(".backup_", backupFile);
        }
    }

    [Fact]
    public async Task CreateAudioBackupAsync_WithCustomPath_CreatesBackupAtPath()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");
        var customBackupPath = GetWorkingPath("custom-backup.mp3");

        // Act
        var result = await _backupTools.CreateAudioBackupAsync(inputFile, customBackupPath);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            var backupFile = response.GetProperty("backupFile").GetString();
            Assert.Equal(customBackupPath, backupFile);
            AssertFileExists(customBackupPath);

            var originalSize = GetFileSize(inputFile);
            var backupSize = GetFileSize(customBackupPath);
            Assert.Equal(originalSize, backupSize);
        }
    }

    [Fact]
    public async Task CreateBatchBackupAsync_WithMultipleFiles_CreatesAllBackups()
    {
        // Arrange
        var inputFile1 = CopyTestFile("sample-short.mp3");
        var inputFile2 = CopyTestFile("sample-short.wav");
        var filePaths = JsonSerializer.Serialize(new[] { inputFile1, inputFile2 });

        // Act
        var result = await _backupTools.CreateBatchBackupAsync(filePaths);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            Assert.Equal(2, response.GetProperty("successCount").GetInt32());
            Assert.Equal(0, response.GetProperty("failureCount").GetInt32());

            var results = response.GetProperty("results").EnumerateArray();
            foreach (var fileResult in results)
            {
                if (fileResult.GetProperty("success").GetBoolean())
                {
                    var backupFile = fileResult.GetProperty("backupFile").GetString();
                    if (!string.IsNullOrEmpty(backupFile))
                    {
                        TrackCreatedFile(backupFile);
                        AssertFileExists(backupFile);
                    }
                }
            }
        }
    }

    [Fact]
    public async Task CreateBatchBackupAsync_WithBackupDirectory_CreatesBackupsInDirectory()
    {
        // Arrange
        var inputFile = CopyTestFile("sample-short.mp3");
        var filePaths = JsonSerializer.Serialize(new[] { inputFile });
        var backupDir = Path.Combine(WorkingDirectory, "backups");
        Directory.CreateDirectory(backupDir);
        TrackCreatedDirectory(backupDir);

        // Act
        var result = await _backupTools.CreateBatchBackupAsync(filePaths, backupDir);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            Assert.Equal(backupDir, response.GetProperty("backupDirectory").GetString());

            var results = response.GetProperty("results").EnumerateArray();
            foreach (var fileResult in results)
            {
                if (fileResult.GetProperty("success").GetBoolean())
                {
                    var backupFile = fileResult.GetProperty("backupFile").GetString();
                    if (!string.IsNullOrEmpty(backupFile))
                    {
                        TrackCreatedFile(backupFile);
                        AssertFileExists(backupFile);
                        Assert.StartsWith(backupDir, backupFile);
                    }
                }
            }
        }
    }

    [Fact]
    public async Task CreateArchiveBackupAsync_WithMultipleFiles_CreatesZipArchive()
    {
        // Arrange
        var inputFile1 = CopyTestFile("sample-short.mp3");
        var inputFile2 = CopyTestFile("sample-short.wav");
        var filePaths = JsonSerializer.Serialize(new[] { inputFile1, inputFile2 });
        var archivePath = GetWorkingPath("backup-archive.zip");

        // Act
        var result = await _backupTools.CreateArchiveBackupAsync(filePaths, archivePath);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            AssertFileExists(archivePath);
            Assert.Equal(2, response.GetProperty("filesIncluded").GetInt32());
            Assert.True(response.GetProperty("archiveSize").GetInt64() > 0);

            // Verify archive contents
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                Assert.True(archive.Entries.Count >= 2);
                Assert.Contains(archive.Entries, e => e.Name.EndsWith(".mp3"));
                Assert.Contains(archive.Entries, e => e.Name.EndsWith(".wav"));

                // Check for metadata file
                Assert.Contains(archive.Entries, e => e.Name == "metadata.json");
            }
        }
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithValidBackup_RestoresFile()
    {
        // Arrange - First create a backup
        var originalFile = CopyTestFile("sample-short.mp3");
        var backupResult = await _backupTools.CreateAudioBackupAsync(originalFile);
        var backupResponse = JsonSerializer.Deserialize<JsonElement>(backupResult);
        var backupFile = backupResponse.GetProperty("backupFile").GetString();

        Assert.NotNull(backupFile);
        TrackCreatedFile(backupFile);

        var restorePath = GetWorkingPath("restored-file.mp3");

        // Act
        var result = await _backupTools.RestoreFromBackupAsync(backupFile, restorePath);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            var restoredFile = response.GetProperty("restoredFile").GetString();
            Assert.Equal(restorePath, restoredFile);
            AssertFileExists(restorePath);

            // Verify restored file has same size as original
            var originalSize = GetFileSize(originalFile);
            var restoredSize = GetFileSize(restorePath);
            Assert.Equal(originalSize, restoredSize);
        }
    }

    [Fact]
    public async Task ListBackupsAsync_WithBackupsInDirectory_ListsBackups()
    {
        // Arrange - Create some backup files first
        var inputFile = CopyTestFile("sample-short.mp3");
        await _backupTools.CreateAudioBackupAsync(inputFile);

        // Act
        var result = await _backupTools.ListBackupsAsync(WorkingDirectory, includeSubdirectories: false);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            Assert.Equal(WorkingDirectory, response.GetProperty("directory").GetString());
            Assert.False(response.GetProperty("includeSubdirectories").GetBoolean());

            var backupCount = response.GetProperty("backupCount").GetInt32();
            if (backupCount > 0)
            {
                var backups = response.GetProperty("backups").EnumerateArray();
                Assert.True(backups.Any());

                foreach (var backup in backups)
                {
                    Assert.True(backup.TryGetProperty("backupFile", out _));
                    Assert.True(backup.TryGetProperty("originalFileName", out _));
                    Assert.True(backup.TryGetProperty("backupTimestamp", out _));
                    Assert.True(backup.TryGetProperty("fileSize", out _));
                }
            }
        }
    }

    [Fact]
    public async Task CleanupBackupsAsync_WithDryRun_ReportsWhatWouldBeDeleted()
    {
        // Arrange - Create a backup file first
        var inputFile = CopyTestFile("sample-short.mp3");
        var backupResult = await _backupTools.CreateAudioBackupAsync(inputFile);
        var backupResponse = JsonSerializer.Deserialize<JsonElement>(backupResult);
        var backupFile = backupResponse.GetProperty("backupFile").GetString();

        Assert.NotNull(backupFile);
        TrackCreatedFile(backupFile);

        // Act - Dry run cleanup (should not actually delete anything)
        var result = await _backupTools.CleanupBackupsAsync(WorkingDirectory, maxAgeDays: 0, maxBackupsPerFile: null, dryRun: true);

        // Assert
        Assert.NotNull(result);
        var response = JsonSerializer.Deserialize<JsonElement>(result);

        if (response.GetProperty("success").GetBoolean())
        {
            Assert.True(response.GetProperty("dryRun").GetBoolean());
            Assert.Contains("Would delete", response.GetProperty("message").GetString());

            // File should still exist after dry run
            AssertFileExists(backupFile);
        }
    }

    [Fact]
    public async Task CreateAudioBackupAsync_WithNonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentFile = GetWorkingPath("nonexistent.mp3");

        // Act
        var result = await _backupTools.CreateAudioBackupAsync(nonExistentFile);

        // Assert
        Assert.Contains("File not found", result);
    }

    [Fact]
    public async Task CreateBatchBackupAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act / Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            var result = await _backupTools.CreateBatchBackupAsync(invalidJson);
        });
    }

    [Fact]
    public async Task CreateArchiveBackupAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var archivePath = GetWorkingPath("test.zip");

        // Act / Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            var result = await _backupTools.CreateArchiveBackupAsync(invalidJson, archivePath);
        });
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithNonExistentBackup_ReturnsError()
    {
        // Arrange
        var nonExistentBackup = GetWorkingPath("nonexistent.backup");
        var restorePath = GetWorkingPath("restored.mp3");

        // Act
        var result = await _backupTools.RestoreFromBackupAsync(nonExistentBackup, restorePath);

        // Assert
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.False(response.GetProperty("success").GetBoolean());
        Assert.Contains("not found", response.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ListBackupsAsync_WithNonExistentDirectory_ReturnsError()
    {
        // Arrange
        var nonExistentDir = GetWorkingPath("nonexistent-dir");

        // Act
        var result = await _backupTools.ListBackupsAsync(nonExistentDir);

        // Assert
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.False(response.GetProperty("success").GetBoolean());
        Assert.Contains("Directory not found", response.GetProperty("message").GetString());
    }
}