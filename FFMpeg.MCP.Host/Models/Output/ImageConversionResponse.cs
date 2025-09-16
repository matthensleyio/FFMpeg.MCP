using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class ImageConversionResponse
{
    public string? Message { get; set; }
    public List<string>? OutputFiles { get; set; }
    public ImageConversionInfo? Conversion { get; set; }
}
