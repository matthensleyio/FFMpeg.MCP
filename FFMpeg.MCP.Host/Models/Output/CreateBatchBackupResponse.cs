using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class CreateBatchBackupResponse
{
    public string? Message { get; set; }
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string? BackupDirectory { get; set; }
    public string? BackupCreated { get; set; }
    public List<BatchBackupResult>? Results { get; set; }
}
