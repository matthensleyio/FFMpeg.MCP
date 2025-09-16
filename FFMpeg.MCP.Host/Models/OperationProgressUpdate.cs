using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models;

public class OperationProgressUpdate
{
    public string OperationId { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
    public Dictionary<string, object>? AdditionalData { get; set; }
}