using System;
using System.Collections.Generic;
using FFMpeg.MCP.Host.Models.Output;

namespace FFMpeg.MCP.Host.Models.Output;

public class OperationProgressUpdate
{
    public string OperationId { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
    public Dictionary<string, object>? AdditionalData { get; set; }
}
