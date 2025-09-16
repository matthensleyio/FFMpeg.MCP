namespace FFMpeg.MCP.Host.Models.Output;

public class BatchBackupResult
{
    public string? OriginalFile { get; set; }
    public string? BackupFile { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}
