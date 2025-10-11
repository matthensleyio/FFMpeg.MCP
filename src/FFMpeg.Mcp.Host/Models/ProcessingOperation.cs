using System.Text.Json.Serialization;

namespace FFMpegMcpServer.Models;

public class ProcessingOperation
{
    [JsonPropertyName("operationId")]
    public Guid OperationId { get; set; }

    [JsonPropertyName("operationType")]
    public OperationType OperationType { get; set; }

    [JsonPropertyName("inputFiles")]
    public List<string> InputFiles { get; set; } = [];

    [JsonPropertyName("outputFile")]
    public required string OutputFile { get; set; }

    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }

    [JsonPropertyName("status")]
    public OperationStatus Status { get; set; }

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("startTime")]
    public DateTime? StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    public ProcessingOperation()
    {
        OperationId = Guid.NewGuid();
        Status = OperationStatus.Queued;
        Progress = 0;
        OutputFile = string.Empty; // Will be set by the parameterized constructor or object initializer
    }

    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    public ProcessingOperation(OperationType operationType, string outputFile, List<string>? inputFiles = null)
        : this()
    {
        OperationType = operationType;
        OutputFile = outputFile;
        InputFiles = inputFiles ?? [];
    }
}