using System.Text.Json.Serialization;

namespace FFMpegMcpServer.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
    Cancelled
}