namespace FFMpeg.MCP.Host.Models.Output;

public class CreateAudioBackupResponse
{
    public string? Message { get; set; }
    public string? OriginalFile { get; set; }
    public string? BackupFile { get; set; }
    public AudioFileInfoResponse? FileInfo { get; set; }
    public string? BackupCreated { get; set; }
}
