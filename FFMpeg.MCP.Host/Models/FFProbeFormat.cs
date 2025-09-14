namespace FFMpeg.MCP.Host.Models;

public class FFProbeFormat
{
    [System.Text.Json.Serialization.JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("nb_streams")]
    public int? NbStreams { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("format_name")]
    public string? FormatName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("format_long_name")]
    public string? FormatLongName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("size")]
    public string? Size { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("bit_rate")]
    public string? BitRate { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("probe_score")]
    public int? ProbeScore { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; set; }
}
