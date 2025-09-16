using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Input;

public class ConversionOptions
{
    public string? OutputFormat { get; set; }
    public int? Quality { get; set; }
    public int? Bitrate { get; set; }
    public string? Codec { get; set; }
    public Dictionary<string, string> CustomOptions { get; set; } = new();
}
