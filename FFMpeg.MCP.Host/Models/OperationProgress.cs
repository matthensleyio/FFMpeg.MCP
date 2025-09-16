using System;
using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models;

public class OperationProgress
{
    public string OperationId { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public OperationStatus Status { get; set; }
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
    public double PercentComplete => TotalSteps > 0 ? (double)CurrentStep / TotalSteps * 100 : 0;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<string> OutputFiles { get; set; } = new();
}