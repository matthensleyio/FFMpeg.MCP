using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class ConcatenationResponse
{
    public string? Message { get; set; }
    public List<string>? OutputFiles { get; set; }
    public ConcatenationInfo? Concatenation { get; set; }
}
