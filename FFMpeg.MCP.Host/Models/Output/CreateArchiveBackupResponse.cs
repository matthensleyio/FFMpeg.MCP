namespace FFMpeg.MCP.Host.Models.Output;

public class CreateArchiveBackupResponse
{
    public string? Message { get; set; }
    public string? ArchivePath { get; set; }
    public long ArchiveSize { get; set; }
    public int FilesIncluded { get; set; }
    public string? ArchiveCreated { get; set; }
}
