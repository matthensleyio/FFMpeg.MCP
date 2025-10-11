using FFMpeg.Mcp.Host.Models;
using FFMpeg.Mcp.Host.Services;
using FFMpeg.Mcp.Host.Tools;
using FluentAssertions;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using NSubstitute;
using Xunit;

namespace FFMpeg.Mcp.Host.UnitTests.Tools;

public class AudioConversionToolsTests
{
    private readonly IAudioProcessingService _audioService;
    private readonly McpServer _mcpServer;
    private readonly RequestContext<CallToolRequestParams> _context;

    public AudioConversionToolsTests()
    {
        _audioService = Substitute.For<IAudioProcessingService>();
        _mcpServer = Substitute.For<McpServer>();

        var jsonRpcRequest = new JsonRpcRequest
        {
            Method = "tools/call",
            Id = new RequestId("1")
        };

        _context = new RequestContext<CallToolRequestParams>(_mcpServer, jsonRpcRequest)
        {
            Params = new CallToolRequestParams { Name = "test_tool" }
        };
    }

    [Fact]
    public async Task ConvertAudio_WithValidParameters_ReturnsSuccessMessage()
    {
        var expectedResult = new OperationResult
        {
            OperationId = Guid.NewGuid(),
            Success = true,
            OutputFilePath = "C:\\output.mp3",
            ProcessingTime = TimeSpan.FromSeconds(2.5),
            OutputFileSize = 2048000
        };

        _audioService.ConvertAudioAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int?>(),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await AudioConversionTools.ConvertAudio(
            _audioService,
            _mcpServer,
            _context,
            "C:\\input.wav",
            "C:\\output.mp3",
            "mp3",
            192,
            44100);

        result.Should().Contain("Successfully converted");
        result.Should().Contain("C:\\output.mp3");
        result.Should().Contain("2.50s");
        result.Should().Contain("2000.00 KB");
    }

    [Fact]
    public async Task ConvertAudio_WithFailure_ReturnsErrorMessage()
    {
        var expectedResult = new OperationResult
        {
            OperationId = Guid.NewGuid(),
            Success = false,
            ErrorMessage = "Invalid format specified",
            ProcessingTime = TimeSpan.Zero,
            OutputFileSize = 0
        };

        _audioService.ConvertAudioAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int?>(),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await AudioConversionTools.ConvertAudio(
            _audioService,
            _mcpServer,
            _context,
            "C:\\input.wav",
            "C:\\output.mp3",
            "mp3");

        result.Should().Contain("Failed to convert");
        result.Should().Contain("Invalid format specified");
    }

    [Fact]
    public async Task ConvertAudio_WithProgressToken_CompletesSuccessfully()
    {
        _context.Params!.ProgressToken = new ProgressToken("test-token");

        var expectedResult = new OperationResult
        {
            OperationId = Guid.NewGuid(),
            Success = true,
            OutputFilePath = "C:\\output.mp3",
            ProcessingTime = TimeSpan.FromSeconds(1),
            OutputFileSize = 1024000
        };

        _audioService.ConvertAudioAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int?>(),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await AudioConversionTools.ConvertAudio(
            _audioService,
            _mcpServer,
            _context,
            "C:\\input.wav",
            "C:\\output.mp3",
            "mp3");

        result.Should().Contain("Successfully converted");
    }

    [Fact]
    public async Task GetAudioInfo_ReturnsFormattedInfo()
    {
        var expectedInfo = new AudioFileInfo
        {
            FilePath = "C:\\test.mp3",
            Format = "mp3",
            Duration = TimeSpan.FromSeconds(180.5),
            Codec = "mp3",
            SampleRate = 44100,
            Bitrate = 192000,
            Channels = 2,
            FileSize = 4096000
        };

        _audioService.GetAudioInfoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedInfo);

        var result = await AudioConversionTools.GetAudioInfo(_audioService, "C:\\test.mp3");

        result.Should().Contain("File: C:\\test.mp3");
        result.Should().Contain("Format: mp3");
        result.Should().Contain("Duration: 180.50s");
        result.Should().Contain("Codec: mp3");
        result.Should().Contain("Sample Rate: 44100 Hz");
        result.Should().Contain("Bitrate: 192 kbps");
        result.Should().Contain("Channels: 2");
        result.Should().Contain("File Size: 4000.00 KB");
    }

    [Fact]
    public async Task TrimAudio_WithValidParameters_ReturnsSuccessMessage()
    {
        var expectedResult = new OperationResult
        {
            OperationId = Guid.NewGuid(),
            Success = true,
            OutputFilePath = "C:\\trimmed.wav",
            ProcessingTime = TimeSpan.FromSeconds(1.5),
            OutputFileSize = 512000
        };

        _audioService.TrimAudioAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await AudioConversionTools.TrimAudio(
            _audioService,
            _mcpServer,
            _context,
            "C:\\input.wav",
            "C:\\trimmed.wav",
            10.0,
            30.0);

        result.Should().Contain("Successfully trimmed");
        result.Should().Contain("C:\\trimmed.wav");
        result.Should().Contain("1.50s");
    }

    [Fact]
    public async Task TrimAudio_WithFailure_ReturnsErrorMessage()
    {
        var expectedResult = new OperationResult
        {
            OperationId = Guid.NewGuid(),
            Success = false,
            ErrorMessage = "Start time exceeds duration",
            ProcessingTime = TimeSpan.Zero,
            OutputFileSize = 0
        };

        _audioService.TrimAudioAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await AudioConversionTools.TrimAudio(
            _audioService,
            _mcpServer,
            _context,
            "C:\\input.wav",
            "C:\\trimmed.wav",
            100.0,
            200.0);

        result.Should().Contain("Failed to trim");
        result.Should().Contain("Start time exceeds duration");
    }

    [Fact]
    public async Task AdjustAudioQuality_WithValidParameters_ReturnsSuccessMessage()
    {
        var expectedResult = new OperationResult
        {
            OperationId = Guid.NewGuid(),
            Success = true,
            OutputFilePath = "C:\\adjusted.mp3",
            ProcessingTime = TimeSpan.FromSeconds(3.0),
            OutputFileSize = 1536000
        };

        _audioService.AdjustQualityAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int?>(),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await AudioConversionTools.AdjustAudioQuality(
            _audioService,
            _mcpServer,
            _context,
            "C:\\input.mp3",
            "C:\\adjusted.mp3",
            128,
            48000);

        result.Should().Contain("Successfully adjusted");
        result.Should().Contain("C:\\adjusted.mp3");
        result.Should().Contain("3.00s");
    }

    [Fact]
    public async Task AdjustAudioQuality_WithNullParameters_CallsServiceCorrectly()
    {
        var expectedResult = new OperationResult
        {
            OperationId = Guid.NewGuid(),
            Success = true,
            OutputFilePath = "C:\\adjusted.mp3",
            ProcessingTime = TimeSpan.FromSeconds(1.0),
            OutputFileSize = 1024000
        };

        _audioService.AdjustQualityAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int?>(),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        await AudioConversionTools.AdjustAudioQuality(
            _audioService,
            _mcpServer,
            _context,
            "C:\\input.mp3",
            "C:\\adjusted.mp3");

        await _audioService.Received(1).AdjustQualityAsync(
            "C:\\input.mp3",
            "C:\\adjusted.mp3",
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExtractAudioChannel_WithValidParameters_ReturnsSuccessMessage()
    {
        var expectedResult = new OperationResult
        {
            OperationId = Guid.NewGuid(),
            Success = true,
            OutputFilePath = "C:\\left.wav",
            ProcessingTime = TimeSpan.FromSeconds(2.0),
            OutputFileSize = 768000
        };

        _audioService.ExtractChannelAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await AudioConversionTools.ExtractAudioChannel(
            _audioService,
            _mcpServer,
            _context,
            "C:\\stereo.wav",
            "C:\\left.wav",
            "0");

        result.Should().Contain("Successfully extracted channel 0");
        result.Should().Contain("C:\\left.wav");
        result.Should().Contain("2.00s");
    }

    [Fact]
    public async Task ExtractAudioChannel_WithFailure_ReturnsErrorMessage()
    {
        var expectedResult = new OperationResult
        {
            OperationId = Guid.NewGuid(),
            Success = false,
            ErrorMessage = "Channel index out of range",
            ProcessingTime = TimeSpan.Zero,
            OutputFileSize = 0
        };

        _audioService.ExtractChannelAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await AudioConversionTools.ExtractAudioChannel(
            _audioService,
            _mcpServer,
            _context,
            "C:\\stereo.wav",
            "C:\\left.wav",
            "5");

        result.Should().Contain("Failed to extract channel");
        result.Should().Contain("Channel index out of range");
    }

    [Fact]
    public async Task MergeAudioFiles_WithValidParameters_ReturnsSuccessMessage()
    {
        var expectedResult = new OperationResult
        {
            OperationId = Guid.NewGuid(),
            Success = true,
            OutputFilePath = "C:\\merged.mp3",
            ProcessingTime = TimeSpan.FromSeconds(5.0),
            OutputFileSize = 4096000
        };

        _audioService.MergeAudioAsync(
            Arg.Any<List<string>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await AudioConversionTools.MergeAudioFiles(
            _audioService,
            _mcpServer,
            _context,
            "C:\\file1.mp3, C:\\file2.mp3, C:\\file3.mp3",
            "C:\\merged.mp3",
            "mp3");

        result.Should().Contain("Successfully merged 3 audio files");
        result.Should().Contain("C:\\merged.mp3");
        result.Should().Contain("5.00s");
    }

    [Fact]
    public async Task MergeAudioFiles_ParsesCsvCorrectly()
    {
        var expectedResult = new OperationResult
        {
            OperationId = Guid.NewGuid(),
            Success = true,
            OutputFilePath = "C:\\merged.mp3",
            ProcessingTime = TimeSpan.FromSeconds(1.0),
            OutputFileSize = 1024000
        };

        _audioService.MergeAudioAsync(
            Arg.Any<List<string>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        await AudioConversionTools.MergeAudioFiles(
            _audioService,
            _mcpServer,
            _context,
            "C:\\file1.mp3,C:\\file2.mp3,  C:\\file3.mp3  ",
            "C:\\merged.mp3",
            "mp3");

        await _audioService.Received(1).MergeAudioAsync(
            Arg.Is<List<string>>(list =>
                list.Count == 3 &&
                list[0] == "C:\\file1.mp3" &&
                list[1] == "C:\\file2.mp3" &&
                list[2] == "C:\\file3.mp3"),
            "C:\\merged.mp3",
            "mp3",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergeAudioFiles_WithFailure_ReturnsErrorMessage()
    {
        var expectedResult = new OperationResult
        {
            OperationId = Guid.NewGuid(),
            Success = false,
            ErrorMessage = "Input files have incompatible formats",
            ProcessingTime = TimeSpan.Zero,
            OutputFileSize = 0
        };

        _audioService.MergeAudioAsync(
            Arg.Any<List<string>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await AudioConversionTools.MergeAudioFiles(
            _audioService,
            _mcpServer,
            _context,
            "C:\\file1.mp3, C:\\file2.wav",
            "C:\\merged.mp3",
            "mp3");

        result.Should().Contain("Failed to merge");
        result.Should().Contain("Input files have incompatible formats");
    }

}
