using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class SplitWithProgressResponse
{
    public string OperationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int TotalChapters { get; set; }
    public bool IsNewOperation { get; set; }
    public int FilesCreated { get; set; }
    public List<string> OutputFiles { get; set; } = new();
}
