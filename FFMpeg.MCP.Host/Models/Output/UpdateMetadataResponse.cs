namespace FFMpeg.MCP.Host.Models.Output;

public class UpdateMetadataResponse
{
    public string? Message { get; set; }
    public string[]? OutputFiles { get; set; }
    public string[]? UpdatedFields { get; set; }
}
