namespace FFMpeg.MCP.Host.Models.Output;

public class BackupInfo
{
    public string? BackupFile { get; set; }
    public string? OriginalFileName { get; set; }
    public string? BackupTimestamp { get; set; }
    public long FileSize { get; set; }
    public string? Created { get; set; }
    public string? Modified { get; set; }
}
