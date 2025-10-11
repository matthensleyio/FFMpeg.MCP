using FFMpeg.Mcp.Host.Validators;
using FluentAssertions;

namespace FFMpeg.Mcp.Host.UnitTests.Validators;

public class FilePathValidatorTests
{
    [Fact]
    public void ValidateInputPath_WithNullPath_ThrowsArgumentException()
    {
        Action act = () => FilePathValidator.ValidateInputPath(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateInputPath_WithEmptyPath_ThrowsArgumentException()
    {
        Action act = () => FilePathValidator.ValidateInputPath("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateOutputPath_WithNullPath_ThrowsArgumentException()
    {
        Action act = () => FilePathValidator.ValidateOutputPath(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsValidPath_WithValidPath_ReturnsTrue()
    {
        var result = FilePathValidator.IsValidPath("/valid/path/file.mp3");
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidPath_WithPathContainingSpaces_ReturnsTrue()
    {
        var result = FilePathValidator.IsValidPath("/path with spaces/file.mp3");
        result.Should().BeTrue();
    }
}
