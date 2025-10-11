using System.Text.Json.Serialization;

namespace FFMpeg.Mcp.Host.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationType
{
    Convert,
    GetInfo,
    Trim,
    AdjustQuality,
    ExtractChannels,
    Merge
}