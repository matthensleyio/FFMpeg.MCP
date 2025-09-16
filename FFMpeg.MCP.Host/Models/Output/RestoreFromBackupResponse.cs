namespace FFMpeg.MCP.Host.Models.Output;

public class RestoreFromBackupResponse
{
    public string? Message { get; set; }
    public string? BackupFile { get; set; }
    public string? RestoredFile { get; set; }
    public AudioFileInfoResponse? FileInfo { get; set; }
    public string? RestoredAt { get; set; }
}
