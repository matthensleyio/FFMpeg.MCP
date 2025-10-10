using System.Text.Json.Serialization;

namespace FFMpegMcpServer.Models;

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