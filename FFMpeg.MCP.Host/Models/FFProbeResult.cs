using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FFMpeg.MCP.Host.Models;

public class FFProbeResult
{
    [JsonPropertyName("format")]
    public FFProbeFormat? Format { get; set; }

    [JsonPropertyName("chapters")]
    public List<FFProbeChapter> Chapters { get; set; } = new();
}

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

public class FFProbeChapterTags
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}
