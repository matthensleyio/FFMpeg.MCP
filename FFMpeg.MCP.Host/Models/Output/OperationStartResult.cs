namespace FFMpeg.MCP.Host.Models.Output;

public class OperationStartResult
{
    public string OperationId { get; set; } = string.Empty;
    public bool IsNewOperation { get; set; }
    public string Message { get; set; } = string.Empty;
}
