using System.Text.Json.Serialization;

namespace FFMpeg.MCP.Host.Models.Output;

public class FFProbeChapterTags
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}
