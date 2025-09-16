using System.Text.Json.Serialization;

namespace FFMpeg.MCP.Host.Models.Output;

public class FFProbeChapter
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("time_base")]
    public string? TimeBase { get; set; }

    [JsonPropertyName("start")]
    public long Start { get; set; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    [JsonPropertyName("end")]
    public long End { get; set; }

    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    [JsonPropertyName("tags")]
    public FFProbeChapterTags? Tags { get; set; }
}
