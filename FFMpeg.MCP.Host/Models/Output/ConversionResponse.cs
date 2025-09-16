using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class ConversionResponse
{
    public string? Message { get; set; }
    public List<string>? OutputFiles { get; set; }
    public ConversionInfo? Conversion { get; set; }
}
