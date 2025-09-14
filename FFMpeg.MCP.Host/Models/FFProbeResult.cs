namespace FFMpeg.MCP.Host.Models;

public class FFProbeResult
{
    [System.Text.Json.Serialization.JsonPropertyName("format")]
    public FFProbeFormat? Format { get; set; }
}
