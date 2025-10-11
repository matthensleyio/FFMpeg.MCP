using System.Text.Json.Serialization;

namespace FFMpegMcpServer.Models;

public record OperationProgress
{
    [JsonPropertyName("operationId")]
    public required Guid OperationId { get; init; }

    [JsonPropertyName("progressPercentage")]
    public required int ProgressPercentage { get; init; }

    [JsonPropertyName("statusMessage")]
    public required string StatusMessage { get; init; }

    [JsonPropertyName("currentStage")]
    public string? CurrentStage { get; init; }

    [JsonPropertyName("timestamp")]
    public required DateTime Timestamp { get; init; }
}