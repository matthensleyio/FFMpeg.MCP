using System.Text.Json.Serialization;

namespace FFMpeg.Mcp.Host.Models;

public record OperationResult
{
    [JsonPropertyName("operationId")]
    public required Guid OperationId { get; init; }

    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    [JsonPropertyName("outputFilePath")]
    public string? OutputFilePath { get; init; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("processingTime")]
    public required TimeSpan ProcessingTime { get; init; }

    [JsonPropertyName("outputFileSize")]
    public long? OutputFileSize { get; init; }

    [JsonPropertyName("warningMessages")]
    public List<string>? WarningMessages { get; init; }
}