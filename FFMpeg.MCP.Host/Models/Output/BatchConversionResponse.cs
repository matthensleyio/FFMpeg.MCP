using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class BatchConversionResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BatchConversionResult>? Results { get; set; }
}
