namespace FFMpeg.MCP.Host.Models.Output;

public class ProcessExecutionResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
