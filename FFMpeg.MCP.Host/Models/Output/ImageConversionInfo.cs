namespace FFMpeg.MCP.Host.Models.Output;

public class ImageConversionInfo
{
    public string? InputFormat { get; set; }
    public string? OutputFormat { get; set; }
    public long? InputSize { get; set; }
    public long? OutputSize { get; set; }
    public string? InputDimensions { get; set; }
    public string? OutputDimensions { get; set; }
    public int? IconSize { get; set; }
}
