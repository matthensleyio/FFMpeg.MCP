using System.Text.Json.Serialization;

namespace FFMpeg.Mcp.Host.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
    Cancelled
}