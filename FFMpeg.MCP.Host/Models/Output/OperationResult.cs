using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> OutputFiles { get; set; } = new();
    public string? ErrorDetails { get; set; }
}
