using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class SupportedFormatsResponse
{
    public List<string>? SupportedFormats { get; set; }
    public int Count { get; set; }
}
