using FFMpegMcpServer.Validators;
using FluentAssertions;

namespace FFMpegMcpServer.UnitTests.Validators;

public class AudioParameterValidatorTests
{
    [Theory]
    [InlineData("mp3")]
    [InlineData("wav")]
    [InlineData("aac")]
    [InlineData("flac")]
    [InlineData("ogg")]
    [InlineData("m4a")]
    public void ValidateFormat_WithValidFormat_DoesNotThrow(string format)
    {
        Action act = () => AudioParameterValidator.ValidateFormat(format);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateFormat_WithInvalidFormat_ThrowsArgumentException()
    {
        Action act = () => AudioParameterValidator.ValidateFormat("invalid");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(64)]
    [InlineData(128)]
    [InlineData(192)]
    [InlineData(320)]
    public void ValidateBitrate_WithValidBitrate_DoesNotThrow(int bitrate)
    {
        Action act = () => AudioParameterValidator.ValidateBitrate(bitrate);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(32)]
    [InlineData(400)]
    public void ValidateBitrate_WithInvalidBitrate_ThrowsArgumentException(int bitrate)
    {
        Action act = () => AudioParameterValidator.ValidateBitrate(bitrate);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(22050)]
    [InlineData(44100)]
    [InlineData(48000)]
    [InlineData(96000)]
    public void ValidateSampleRate_WithValidRate_DoesNotThrow(int sampleRate)
    {
        Action act = () => AudioParameterValidator.ValidateSampleRate(sampleRate);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTimestamps_WithValidRange_DoesNotThrow()
    {
        var start = TimeSpan.FromSeconds(10);
        var end = TimeSpan.FromSeconds(30);
        var duration = TimeSpan.FromMinutes(1);

        Action act = () => AudioParameterValidator.ValidateTimestamps(start, end, duration);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTimestamps_WithStartAfterEnd_ThrowsArgumentException()
    {
        var start = TimeSpan.FromSeconds(30);
        var end = TimeSpan.FromSeconds(10);
        var duration = TimeSpan.FromMinutes(1);

        Action act = () => AudioParameterValidator.ValidateTimestamps(start, end, duration);
        act.Should().Throw<ArgumentException>();
    }
}
