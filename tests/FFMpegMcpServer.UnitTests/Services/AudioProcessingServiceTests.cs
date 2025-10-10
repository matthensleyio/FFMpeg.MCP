using FFMpegMcpServer.Models;
using FFMpegMcpServer.Services;
using FluentAssertions;
using Xunit;

namespace FFMpegMcpServer.UnitTests.Services;

public class AudioProcessingServiceTests
{
    private readonly AudioProcessingService _sut;

    public AudioProcessingServiceTests()
    {
        _sut = new AudioProcessingService();
    }

    [Fact]
    public async Task GetAudioInfoAsync_WithNullPath_ThrowsArgumentException()
    {
        var act = async () => await _sut.GetAudioInfoAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAudioInfoAsync_WithEmptyPath_ThrowsArgumentException()
    {
        var act = async () => await _sut.GetAudioInfoAsync(string.Empty, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ConvertAudioAsync_WithNullInputPath_ReturnsFailureResult()
    {
        var result = await _sut.ConvertAudioAsync(null!, "C:\\temp\\output.mp3", "mp3", null, null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ConvertAudioAsync_WithNullOutputPath_ReturnsFailureResult()
    {
        var result = await _sut.ConvertAudioAsync("C:\\temp\\input.wav", null!, "mp3", null, null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TrimAudioAsync_WithNullInputPath_ReturnsFailureResult()
    {
        var result = await _sut.TrimAudioAsync(null!, "C:\\temp\\output.wav", TimeSpan.Zero, TimeSpan.FromSeconds(10), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TrimAudioAsync_WithNullOutputPath_ReturnsFailureResult()
    {
        var result = await _sut.TrimAudioAsync("C:\\temp\\input.wav", null!, TimeSpan.Zero, TimeSpan.FromSeconds(10), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AdjustQualityAsync_WithNullInputPath_ReturnsFailureResult()
    {
        var result = await _sut.AdjustQualityAsync(null!, "C:\\temp\\output.wav", 192, null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AdjustQualityAsync_WithNullOutputPath_ReturnsFailureResult()
    {
        var result = await _sut.AdjustQualityAsync("C:\\temp\\input.wav", null!, 192, null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExtractChannelAsync_WithNullInputPath_ReturnsFailureResult()
    {
        var result = await _sut.ExtractChannelAsync(null!, "C:\\temp\\output.wav", "0", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExtractChannelAsync_WithNullOutputPath_ReturnsFailureResult()
    {
        var result = await _sut.ExtractChannelAsync("C:\\temp\\input.wav", null!, "0", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task MergeAudioAsync_WithNullInputPaths_ReturnsFailureResult()
    {
        var result = await _sut.MergeAudioAsync(null!, "C:\\temp\\output.wav", "mp3", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("At least two input files");
    }

    [Fact]
    public async Task MergeAudioAsync_WithSingleInputFile_ReturnsFailureResult()
    {
        var result = await _sut.MergeAudioAsync(["C:\\temp\\input.wav"], "C:\\temp\\output.wav", "mp3", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("At least two input files");
    }

    [Fact]
    public async Task MergeAudioAsync_WithNullOutputPath_ReturnsFailureResult()
    {
        var result = await _sut.MergeAudioAsync(["C:\\temp\\input1.wav", "C:\\temp\\input2.wav"], null!, "mp3", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
