using FFMpegMcpServer.Models;
using FluentAssertions;

namespace FFMpegMcpServer.UnitTests.Models;

public class AudioFileInfoTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var info = new AudioFileInfo
        {
            FilePath = "/test/audio.mp3",
            Format = "mp3",
            Duration = TimeSpan.FromMinutes(3),
            Codec = "mp3",
            SampleRate = 44100,
            Bitrate = 192000,
            Channels = 2,
            FileSize = 5242880
        };

        info.Should().NotBeNull();
        info.FilePath.Should().Be("/test/audio.mp3");
        info.Format.Should().Be("mp3");
        info.Duration.Should().Be(TimeSpan.FromMinutes(3));
    }
}
