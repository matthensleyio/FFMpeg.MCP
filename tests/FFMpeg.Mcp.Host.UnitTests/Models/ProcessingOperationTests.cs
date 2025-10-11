using FFMpeg.Mcp.Host.Models;
using FluentAssertions;

namespace FFMpeg.Mcp.Host.UnitTests.Models;

public class ProcessingOperationTests
{
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        var operation = new ProcessingOperation { OutputFile = "/output.wav" };

        operation.OperationId.Should().NotBeEmpty();
        operation.Status.Should().Be(OperationStatus.Queued);
        operation.Progress.Should().Be(0);
        operation.OutputFile.Should().Be("/output.wav");
    }

    [Fact]
    public void StateTransition_ToCompleted_Works()
    {
        var operation = new ProcessingOperation
        {
            Status = OperationStatus.Processing,
            OutputFile = "/output.wav"
        };
        
        operation.Status = OperationStatus.Completed;
        operation.Progress = 100;

        operation.Status.Should().Be(OperationStatus.Completed);
        operation.Progress.Should().Be(100);
    }
}
