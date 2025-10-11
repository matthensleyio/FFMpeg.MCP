using System.Text.Json.Serialization;

namespace FFMpeg.Mcp.Host.Models;

public record AudioFileInfo
{
    [JsonPropertyName("filePath")]
    public required string FilePath { get; init; }

    [JsonPropertyName("format")]
    public required string Format { get; init; }

    [JsonPropertyName("duration")]
    public required TimeSpan Duration { get; init; }

    [JsonPropertyName("codec")]
    public required string Codec { get; init; }

    [JsonPropertyName("sampleRate")]
    public required int SampleRate { get; init; }

    [JsonPropertyName("bitrate")]
    public required long Bitrate { get; init; }

    [JsonPropertyName("channels")]
    public required int Channels { get; init; }

    [JsonPropertyName("fileSize")]
    public required long FileSize { get; init; }
}