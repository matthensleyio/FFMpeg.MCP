using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class SplitResponse
{
    public string? Message { get; set; }
    public List<string>? OutputFiles { get; set; }
    public int FilesCreated { get; set; }
}
