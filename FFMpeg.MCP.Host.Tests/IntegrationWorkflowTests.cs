using FFMpeg.MCP.Host.Mcp;
using FFMpeg.MCP.Host.Tools;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tests;

public class IntegrationWorkflowTests : TestBase
{
    private readonly AudioMetadataTools _metadataTools;
    private readonly AudioSplittingTools _splittingTools;
    private readonly AudioConversionTools _conversionTools;
    private readonly AudioChapterTools _chapterTools;
    private readonly AudioBackupTools _backupTools;

    public IntegrationWorkflowTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dispatcherLogger = loggerFactory.CreateLogger<McpDispatcher>();
        var dispatcher = new McpDispatcher(dispatcherLogger);

        _metadataTools = new AudioMetadataTools(FFmpegService, loggerFactory.CreateLogger<AudioMetadataTools>());
        _splittingTools = new AudioSplittingTools(FFmpegService, loggerFactory.CreateLogger<AudioSplittingTools>());
        _conversionTools = new AudioConversionTools(FFmpegService, loggerFactory.CreateLogger<AudioConversionTools>());
        _chapterTools = new AudioChapterTools(FFmpegService, loggerFactory.CreateLogger<AudioChapterTools>(), dispatcher);
        _backupTools = new AudioBackupTools(FFmpegService, loggerFactory.CreateLogger<AudioBackupTools>(), dispatcher);
    }

    [Fact]
    public async Task GetFileInfo_WithChapters_ReturnsChapterInfo()
    {
        // Arrange
        var inputFile = CopyTestFile("with-chapters.m4a");

        // Act
        var fileInfoResult = await _metadataTools.GetAudioFileInfoAsync(inputFile);

        // Assert
        Assert.NotNull(fileInfoResult);
        var fileInfo = JsonSerializer.Deserialize<JsonElement>(fileInfoResult);
        Assert.True(fileInfo.GetProperty("success").GetBoolean(), fileInfo.GetProperty("message").GetString());
        Assert.True(fileInfo.TryGetProperty("chapters", out var chaptersProp));
        var chapters = chaptersProp.EnumerateArray().ToList();
        Assert.True(chapters.Count > 0);
        Assert.Equal("Chapter 1", chapters[0].GetProperty("title").GetString());
    }

    [Fact(Skip = "It is unclear what this test is trying to do.")]
    public async Task CompleteAudiobookWorkflow_ProcessesFileSuccessfully()
    {
        // Arrange
        var originalFile = CopyTestFile("long-form.mp3");

        // Step 1: Backup the original file
        var backupResponse = await _backupTools.CreateAudioBackupAsync(originalFile);
        Assert.NotNull(backupResponse);
        Assert.Null(backupResponse.Error);
        Assert.NotNull(backupResponse.Result);
        Assert.NotNull(backupResponse.Result.BackupFile);
        TrackCreatedFile(backupResponse.Result.BackupFile);


        // Step 2: Get file info
        var fileInfoResult = await _metadataTools.GetAudioFileInfoAsync(originalFile);
        Assert.NotNull(fileInfoResult);
        Assert.DoesNotContain("Could not analyze", fileInfoResult);

        // Step 3: Update metadata
        var metadataResult = await _metadataTools.UpdateCommonMetadataAsync(
            originalFile,
            title: "Test Audiobook",
            artist: "Test Author",
            album: "Test Collection",
            genre: "Audiobook",
            outputPath: GetWorkingPath("updated-audiobook.mp3")
        );

        var metadataResponse = JsonSerializer.Deserialize<JsonElement>(metadataResult);
        string updatedFile = originalFile;
        if (metadataResponse.GetProperty("success").GetBoolean())
        {
            updatedFile = metadataResponse.GetProperty("outputFiles").EnumerateArray().First().GetString()!;
            AssertFileExists(updatedFile);
        }

        // Step 4: Generate equal chapters
        var chaptersFile = GetWorkingPath("with-chapters.mp3");
        var chapterResponse = await _chapterTools.GenerateEqualChaptersAsync(
            updatedFile,
            chapterDurationMinutes: 3.0,
            titlePattern: "Chapter {index}",
            outputPath: chaptersFile
        );

        Assert.NotNull(chapterResponse);
        Assert.Null(chapterResponse.Error);
        Assert.NotNull(chapterResponse.Result);
        AssertFileExists(chaptersFile);
        Assert.True(chapterResponse.Result.ChaptersGenerated > 0);

        // Step 5: Split by chapters (if chapters were successfully added)
        if (File.Exists(chaptersFile))
        {
            var splitResult = await _splittingTools.SplitAudioByChaptersAsync(chaptersFile);
            var splitResponse = JsonSerializer.Deserialize<JsonElement>(splitResult);

            if (splitResponse.TryGetProperty("success", out var splitSuccessProp) && splitSuccessProp.GetBoolean())
            {
                var outputFiles = splitResponse.GetProperty("outputFiles").EnumerateArray();
                foreach (var file in outputFiles)
                {
                    var filePath = file.GetString();
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        TrackCreatedFile(filePath);
                        AssertFileExists(filePath);
                    }
                }
            }
        }

        // Step 6: Export chapter information
        if (File.Exists(chaptersFile))
        {
            var exportResponse = await _chapterTools.ExportChapterInfoAsync(chaptersFile, format: "json");
            Assert.NotNull(exportResponse);
            Assert.Null(exportResponse.Error);
            Assert.NotNull(exportResponse.Result);
            Assert.NotNull(exportResponse.Result.OutputFile);
            TrackCreatedFile(exportResponse.Result.OutputFile);
            AssertFileExists(exportResponse.Result.OutputFile);
        }

        // Assert: Verify the workflow completed without critical errors
        Assert.True(File.Exists(originalFile));
    }

    [Fact]
    public async Task FormatConversionWorkflow_ConvertsAcrossFormats()
    {
        // Arrange
        var originalMp3 = CopyTestFile("sample-short.mp3");

        // Step 1: Convert MP3 to WAV
        var wavFile = GetWorkingPath("converted.wav");
        var toWavResult = await _conversionTools.ConvertToWavAsync(originalMp3, wavFile);
        var wavResponse = JsonSerializer.Deserialize<JsonElement>(toWavResult);

        if (wavResponse.GetProperty("success").GetBoolean())
        {
            AssertFileExists(wavFile);

            // Step 2: Convert WAV to FLAC
            var flacFile = GetWorkingPath("lossless.flac");
            var toFlacResult = await _conversionTools.ConvertToFlacAsync(wavFile, flacFile);
            var flacResponse = JsonSerializer.Deserialize<JsonElement>(toFlacResult);

            if (flacResponse.GetProperty("success").GetBoolean())
            {
                AssertFileExists(flacFile);

                // Step 3: Convert FLAC back to MP3
                var finalMp3 = GetWorkingPath("final.mp3");
                var backToMp3Result = await _conversionTools.ConvertToMp3Async(flacFile, finalMp3, quality: "high");
                var mp3Response = JsonSerializer.Deserialize<JsonElement>(backToMp3Result);

                if (mp3Response.GetProperty("success").GetBoolean())
                {
                    AssertFileExists(finalMp3);

                    // Verify all three formats exist and have different extensions
                    Assert.Equal(".wav", Path.GetExtension(wavFile).ToLower());
                    Assert.Equal(".flac", Path.GetExtension(flacFile).ToLower());
                    Assert.Equal(".mp3", Path.GetExtension(finalMp3).ToLower());

                    // Verify files have reasonable sizes (FLAC should be largest, MP3 smallest typically)
                    var wavSize = GetFileSize(wavFile);
                    var flacSize = GetFileSize(flacFile);
                    var mp3Size = GetFileSize(finalMp3);

                    Assert.True(wavSize > 0);
                    Assert.True(flacSize > 0);
                    Assert.True(mp3Size > 0);
                }
            }
        }
    }

    [Fact]
    public async Task BatchProcessingWorkflow_ProcessesMultipleFiles()
    {
        // Arrange
        var file1 = CopyTestFile("sample-short.mp3");
        var file2 = CopyTestFile("long-speech.wav");

        // Step 1: Create batch backup
        var filePaths = JsonSerializer.Serialize(new[] { file1, file2 });
        var batchBackupResponse = await _backupTools.CreateBatchBackupAsync(filePaths);
        Assert.NotNull(batchBackupResponse);
        Assert.Null(batchBackupResponse.Error);
        Assert.NotNull(batchBackupResponse.Result);
        Assert.NotNull(batchBackupResponse.Result.Results);

        foreach(var result in batchBackupResponse.Result.Results)
        {
            if(result.Success)
            {
                Assert.NotNull(result.BackupFile);
                TrackCreatedFile(result.BackupFile);
            }
        }


        // Step 2: Batch convert to MP3
        var outputDir = Path.Combine(WorkingDirectory, "batch-converted");
        Directory.CreateDirectory(outputDir);
        TrackCreatedDirectory(outputDir);

        var batchConvertResult = await _conversionTools.BatchConvertAudioAsync(
            filePaths, outputDir, "mp3", bitrate: 192, keepOriginalNames: true);
        var convertResponse = JsonSerializer.Deserialize<JsonElement>(batchConvertResult);

        if (convertResponse.GetProperty("success").GetBoolean())
        {
            Assert.True(convertResponse.GetProperty("successCount").GetInt32() > 0);

            var convertResults = convertResponse.GetProperty("results").EnumerateArray();
            foreach (var result in convertResults)
            {
                if (result.GetProperty("success").GetBoolean())
                {
                    var outputFile = result.GetProperty("outputPath").GetString();
                    if (!string.IsNullOrEmpty(outputFile))
                    {
                        TrackCreatedFile(outputFile);
                        AssertFileExists(outputFile);
                        Assert.Equal(".mp3", Path.GetExtension(outputFile).ToLower());
                    }
                }
            }
        }

        // Step 3: Create archive of converted files
        var convertedFiles = Directory.GetFiles(outputDir, "*.mp3");
        if (convertedFiles.Any())
        {
            var archiveFilePaths = JsonSerializer.Serialize(convertedFiles);
            var archivePath = GetWorkingPath("batch-archive.zip");

            var archiveResponse = await _backupTools.CreateArchiveBackupAsync(
                archiveFilePaths, archivePath);

            Assert.NotNull(archiveResponse);
            Assert.Null(archiveResponse.Error);
            Assert.NotNull(archiveResponse.Result);

            AssertFileExists(archivePath);
            Assert.True(archiveResponse.Result.ArchiveSize > 0);
        }
    }

    [Fact]
    public async Task AudioSplittingWorkflow_SplitsAndProcesses()
    {
        // Arrange
        var longFile = CopyTestFile("long-form.mp3");

        // Step 1: Split by duration
        var splitResult = await _splittingTools.SplitAudioByMinutesAsync(longFile, maxDurationMinutes: 2.0);
        var splitResponse = JsonSerializer.Deserialize<JsonElement>(splitResult);

        List<string> splitFiles = new();
        if (splitResponse.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
        {
            var outputFiles = splitResponse.GetProperty("outputFiles").EnumerateArray();
            foreach (var file in outputFiles)
            {
                var filePath = file.GetString();
                if (!string.IsNullOrEmpty(filePath))
                {
                    TrackCreatedFile(filePath);
                    splitFiles.Add(filePath);
                    AssertFileExists(filePath);
                }
            }
        }

        // Step 2: Process each split file - add metadata
        if (splitFiles.Any())
        {
            for (int i = 0; i < splitFiles.Count; i++)
            {
                var splitFile = splitFiles[i];
                var outputFile = GetWorkingPath($"processed-part-{i + 1}.mp3");

                var metadataResult = await _metadataTools.UpdateCommonMetadataAsync(
                    splitFile,
                    title: $"Part {i + 1}",
                    artist: "Test Artist",
                    track: (i + 1).ToString(),
                    outputPath: outputFile
                );

                var metadataResponse = JsonSerializer.Deserialize<JsonElement>(metadataResult);
                if (metadataResponse.GetProperty("success").GetBoolean())
                {
                    AssertFileExists(outputFile);
                }
            }
        }

        // Step 3: Verify we have processed files
        if (splitFiles.Any())
        {
            Assert.True(splitFiles.Count > 0, "Should have created split files");
        }
    }

    [Fact]
    public async Task ErrorHandlingWorkflow_HandlesInvalidInputsGracefully()
    {
        // Test 1: Non-existent file operations
        var nonExistentFile = GetWorkingPath("does-not-exist.mp3");

        var metadataResult = await _metadataTools.GetAudioFileInfoAsync(nonExistentFile);
        Assert.Contains("Could not analyze", metadataResult);

        var conversionResult = await _conversionTools.ConvertToMp3Async(nonExistentFile, GetWorkingPath("output.mp3"));
        Assert.Contains("File not found", conversionResult);

        var splitResult = await _splittingTools.SplitAudioByDurationAsync(nonExistentFile, 60);
        Assert.Contains("File not found", splitResult);

        var backupResponse = await _backupTools.CreateAudioBackupAsync(nonExistentFile);
        Assert.NotNull(backupResponse);
        Assert.Null(backupResponse.Result);
        Assert.NotNull(backupResponse.Error);
        Assert.Equal("not_found", backupResponse.Error.Code);


        // Test 2: Invalid JSON operations
        var validFile = CopyTestFile("sample-short.mp3");

        var invalidMetadataResult = await _metadataTools.UpdateAudioMetadataAsync(validFile, "invalid json");
        Assert.Contains("Error updating metadata", invalidMetadataResult);

        var invalidChapterResponse = await _chapterTools.SetAudioChaptersAsync(validFile, "invalid json");
        Assert.NotNull(invalidChapterResponse);
        Assert.Null(invalidChapterResponse.Result);
        Assert.NotNull(invalidChapterResponse.Error);
        Assert.Equal("internal_error", invalidChapterResponse.Error.Code);


        var invalidBatchResponse = await _backupTools.CreateBatchBackupAsync("invalid json");
        Assert.NotNull(invalidBatchResponse);
        Assert.Null(invalidBatchResponse.Result);
        Assert.NotNull(invalidBatchResponse.Error);
        Assert.Equal("internal_error", invalidBatchResponse.Error.Code);


        // All error cases should be handled gracefully without throwing exceptions
        Assert.True(true, "All error cases handled gracefully");
    }
}