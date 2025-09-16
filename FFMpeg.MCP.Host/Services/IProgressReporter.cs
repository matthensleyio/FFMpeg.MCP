using System;
using System.Threading.Tasks;
using FFMpeg.MCP.Host.Models;

namespace FFMpeg.MCP.Host.Services;

public interface IProgressReporter
{
    Task ReportProgressAsync(OperationProgressUpdate update);
    Task ReportErrorAsync(string operationId, string error);
    Task ReportCompletionAsync(string operationId, OperationResult result);
    Task<OperationProgress?> GetProgressAsync(string operationId);
    event EventHandler<OperationProgressUpdate>? ProgressUpdated;
}