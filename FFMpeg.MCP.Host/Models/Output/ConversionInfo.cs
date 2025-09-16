namespace FFMpeg.MCP.Host.Models.Output;

public class ConversionInfo
{
    public string? InputFormat { get; set; }
    public string? OutputFormat { get; set; }
    public long? InputSize { get; set; }
    public long? OutputSize { get; set; }
    public string? InputDuration { get; set; }
    public string? OutputDuration { get; set; }
    public int? Bitrate { get; set; }
    public string? Codec { get; set; }
    public int? CompressionLevel { get; set; }
    public string? CompressionRatio { get; set; }
    public int? SampleRate { get; set; }
    public int? BitDepth { get; set; }
}
