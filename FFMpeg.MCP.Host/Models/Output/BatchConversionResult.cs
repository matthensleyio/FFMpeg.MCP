namespace FFMpeg.MCP.Host.Models.Output;

public class BatchConversionResult
{
    public string? InputPath { get; set; }
    public string? OutputPath { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}
