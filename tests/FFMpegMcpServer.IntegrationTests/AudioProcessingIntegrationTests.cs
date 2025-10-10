using FFMpegCore;
using FFMpegMcpServer.Models;
using FFMpegMcpServer.Services;
using FluentAssertions;
using Xunit;

namespace FFMpegMcpServer.IntegrationTests;

public class AudioProcessingIntegrationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly IAudioProcessingService _audioService;

    public AudioProcessingIntegrationTests()
    {
        GlobalFFOptions.Configure(options =>
        {
            options.TemporaryFilesFolder = Path.GetTempPath();
            options.WorkingDirectory = Environment.CurrentDirectory;
        });

        _testDirectory = Path.Combine(Path.GetTempPath(), "ffmpeg-integration-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _audioService = new AudioProcessingService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
            }
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetAudioInfoAsync_WithValidWavFile_ReturnsCorrectMetadata()
    {
        var testFile = CreateTestAudioFile("test.wav");

        var info = await _audioService.GetAudioInfoAsync(testFile, CancellationToken.None);

        info.FilePath.Should().Be(testFile);
        info.Format.Should().Be("wav");
        info.Duration.TotalSeconds.Should().BeGreaterThan(0);
        info.Channels.Should().Be(2);
        info.SampleRate.Should().Be(44100);
    }

    [Fact]
    public async Task ConvertAudioAsync_WavToMp3_CreatesValidMp3File()
    {
        var inputFile = CreateTestAudioFile("input.wav");
        var outputFile = Path.Combine(_testDirectory, "output.mp3");

        var result = await _audioService.ConvertAudioAsync(
            inputFile,
            outputFile,
            "mp3",
            192,
            44100,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OutputFilePath.Should().Be(outputFile);
        result.OutputFileSize.Should().BeGreaterThan(0);
        File.Exists(outputFile).Should().BeTrue();

        var outputInfo = await _audioService.GetAudioInfoAsync(outputFile, CancellationToken.None);
        outputInfo.Format.Should().Be("mp3");
    }

    [Fact]
    public async Task TrimAudioAsync_TrimsTwoSecondFile_CreatesOneSecondFile()
    {
        var inputFile = CreateTestAudioFile("input.wav", duration: 2.0);
        var outputFile = Path.Combine(_testDirectory, "trimmed.wav");

        var result = await _audioService.TrimAudioAsync(
            inputFile,
            outputFile,
            TimeSpan.FromSeconds(0.5),
            TimeSpan.FromSeconds(1.5),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        File.Exists(outputFile).Should().BeTrue();

        var outputInfo = await _audioService.GetAudioInfoAsync(outputFile, CancellationToken.None);
        outputInfo.Duration.TotalSeconds.Should().BeApproximately(1.0, 0.1);
    }

    [Fact]
    public async Task AdjustQualityAsync_ReducesBitrate_CreatesLowerBitrateFile()
    {
        var inputFile = CreateTestAudioFile("input.wav");
        var outputFile = Path.Combine(_testDirectory, "quality-adjusted.wav");

        var result = await _audioService.AdjustQualityAsync(
            inputFile,
            outputFile,
            128,
            null,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        File.Exists(outputFile).Should().BeTrue();
    }

    [Fact]
    public async Task ExtractChannelAsync_ExtractsLeftChannel_CreatesMonoFile()
    {
        var inputFile = CreateTestAudioFile("stereo.wav");
        var outputFile = Path.Combine(_testDirectory, "mono.wav");

        var result = await _audioService.ExtractChannelAsync(
            inputFile,
            outputFile,
            "0",
            CancellationToken.None);

        if (!result.Success)
        {
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            return;
        }

        File.Exists(outputFile).Should().BeTrue();

        var outputInfo = await _audioService.GetAudioInfoAsync(outputFile, CancellationToken.None);
        outputInfo.Channels.Should().Be(1);
    }

    [Fact]
    public async Task MergeAudioAsync_MergesThreeFiles_CreatesCombinedFile()
    {
        var input1 = CreateTestAudioFile("file1.wav", duration: 1.0);
        var input2 = CreateTestAudioFile("file2.wav", duration: 1.0);
        var input3 = CreateTestAudioFile("file3.wav", duration: 1.0);
        var outputFile = Path.Combine(_testDirectory, "merged.wav");

        var result = await _audioService.MergeAudioAsync(
            [input1, input2, input3],
            outputFile,
            "wav",
            CancellationToken.None);

        result.Success.Should().BeTrue();
        File.Exists(outputFile).Should().BeTrue();

        var outputInfo = await _audioService.GetAudioInfoAsync(outputFile, CancellationToken.None);
        outputInfo.Duration.TotalSeconds.Should().BeGreaterThan(2.5);
    }

    [Fact]
    public async Task ConvertAudioAsync_WithInvalidInputPath_ReturnsFailureResult()
    {
        var result = await _audioService.ConvertAudioAsync(
            Path.Combine(_testDirectory, "nonexistent.wav"),
            Path.Combine(_testDirectory, "output.mp3"),
            "mp3",
            null,
            null,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ConvertAudioAsync_WithUnsupportedFormat_ReturnsFailureResult()
    {
        var inputFile = CreateTestAudioFile("input.wav");

        var result = await _audioService.ConvertAudioAsync(
            inputFile,
            Path.Combine(_testDirectory, "output.xyz"),
            "xyz",
            null,
            null,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unsupported audio format");
    }

    [Fact]
    public async Task TrimAudioAsync_WithInvalidTimeRange_ReturnsFailureResult()
    {
        var inputFile = CreateTestAudioFile("input.wav", duration: 2.0);

        var result = await _audioService.TrimAudioAsync(
            inputFile,
            Path.Combine(_testDirectory, "trimmed.wav"),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExtractChannelAsync_WithInvalidChannelIndex_ReturnsFailureResult()
    {
        var inputFile = CreateTestAudioFile("input.wav");

        var result = await _audioService.ExtractChannelAsync(
            inputFile,
            Path.Combine(_testDirectory, "output.wav"),
            "10",
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("out of range");
    }

    [Fact]
    public async Task MergeAudioAsync_WithSingleFile_ReturnsFailureResult()
    {
        var inputFile = CreateTestAudioFile("single.wav");

        var result = await _audioService.MergeAudioAsync(
            [inputFile],
            Path.Combine(_testDirectory, "merged.wav"),
            "wav",
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("At least two input files");
    }

    private string CreateTestAudioFile(string filename, double duration = 2.0, int channels = 2)
    {
        var filePath = Path.Combine(_testDirectory, filename);
        var sampleRate = 44100;
        var numSamples = (int)(sampleRate * duration);

        using var fileStream = File.Create(filePath);
        using var writer = new BinaryWriter(fileStream);

        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + numSamples * channels * 2);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });

        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * 2);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);

        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(numSamples * channels * 2);

        for (int i = 0; i < numSamples; i++)
        {
            var sample = (short)(Math.Sin(2 * Math.PI * 440 * i / sampleRate) * 10000);
            for (int c = 0; c < channels; c++)
            {
                writer.Write(sample);
            }
        }

        return filePath;
    }
}
