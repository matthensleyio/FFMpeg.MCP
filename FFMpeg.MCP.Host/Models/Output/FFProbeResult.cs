using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FFMpeg.MCP.Host.Models.Output;

public class FFProbeResult
{
    [JsonPropertyName("format")]
    public FFProbeFormat? Format { get; set; }

    [JsonPropertyName("chapters")]
    public List<FFProbeChapter> Chapters { get; set; } = new();
}
