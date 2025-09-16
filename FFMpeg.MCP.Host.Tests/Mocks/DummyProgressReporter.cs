using System;
using System.Threading.Tasks;
using FFMpeg.MCP.Host.Models.Output;
using FFMpeg.MCP.Host.Services;

namespace FFMpeg.MCP.Host.Tests.Mocks;

public class DummyProgressReporter : IProgressReporter
{
    public event EventHandler<OperationProgressUpdate>? ProgressUpdated;

    public Task ReportProgressAsync(OperationProgressUpdate update) => Task.CompletedTask;
    public Task ReportErrorAsync(string operationId, string error) => Task.CompletedTask;
    public Task ReportCompletionAsync(string operationId, OperationResult result) => Task.CompletedTask;
    public Task<OperationProgress?> GetProgressAsync(string operationId) => Task.FromResult<OperationProgress?>(null);
}
