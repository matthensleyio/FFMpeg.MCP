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

        _metadataTools = new AudioMetadataTools(FFmpegService, loggerFactory.CreateLogger<AudioMetadataTools>(), dispatcher);
        _splittingTools = new AudioSplittingTools(FFmpegService, loggerFactory.CreateLogger<AudioSplittingTools>(), dispatcher);
        _conversionTools = new AudioConversionTools(FFmpegService, loggerFactory.CreateLogger<AudioConversionTools>(), dispatcher);
        _chapterTools = new AudioChapterTools(FFmpegService, loggerFactory.CreateLogger<AudioChapterTools>(), dispatcher);
        _backupTools = new AudioBackupTools(FFmpegService, loggerFactory.CreateLogger<AudioBackupTools>(), dispatcher);
    }

    [Fact]
    public async Task GetFileInfo_WithChapters_ReturnsChapterInfo()
    {
        // Arrange
        var inputFile = CopyTestFile("with-chapters.m4a");

        // Act
        var response = await _metadataTools.GetAudioFileInfoAsync(inputFile);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        Assert.True(response.Result.Chapters.Count > 0);
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
        var fileInfoResponse = await _metadataTools.GetAudioFileInfoAsync(originalFile);
        Assert.NotNull(fileInfoResponse);
        Assert.Null(fileInfoResponse.Error);
        Assert.NotNull(fileInfoResponse.Result);

        // Step 3: Update metadata
        var metadataResponse = await _metadataTools.UpdateCommonMetadataAsync(
            originalFile,
            title: "Test Audiobook",
            outputPath: GetWorkingPath("updated-audiobook.mp3")
        );

        Assert.NotNull(metadataResponse);
        Assert.Null(metadataResponse.Error);
        Assert.NotNull(metadataResponse.Result);
        Assert.NotNull(metadataResponse.Result.OutputFiles);
        var updatedFile = metadataResponse.Result.OutputFiles.First();
        AssertFileExists(updatedFile);


        // Step 4: Generate equal chapters
        var chaptersFile = GetWorkingPath("with-chapters.mp3");
        var chapterResponse = await _chapterTools.GenerateEqualChaptersAsync(
            updatedFile,
            chapterDurationMinutes: 3.0,
            outputPath: chaptersFile
        );

        Assert.NotNull(chapterResponse);
        Assert.Null(chapterResponse.Error);
        Assert.NotNull(chapterResponse.Result);
        AssertFileExists(chaptersFile);
        Assert.True(chapterResponse.Result.ChaptersGenerated > 0);

        // Step 5: Split by chapters
        var splitResponse = await _splittingTools.SplitAudioByChaptersAsync(chaptersFile);
        Assert.NotNull(splitResponse);
        Assert.Null(splitResponse.Error);
        Assert.NotNull(splitResponse.Result);
        Assert.True(splitResponse.Result.FilesCreated > 0);
        foreach(var file in splitResponse.Result.OutputFiles)
        {
            TrackCreatedFile(file);
        }


        // Step 6: Export chapter information
        var exportResponse = await _chapterTools.ExportChapterInfoAsync(chaptersFile, format: "json");
        Assert.NotNull(exportResponse);
        Assert.Null(exportResponse.Error);
        Assert.NotNull(exportResponse.Result);
        Assert.NotNull(exportResponse.Result.OutputFile);
        TrackCreatedFile(exportResponse.Result.OutputFile);
        AssertFileExists(exportResponse.Result.OutputFile);

        // Assert: Verify the workflow completed without critical errors
        Assert.True(File.Exists(originalFile));
    }
}