using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class ListBackupsResponse
{
    public string? Message { get; set; }
    public string? Directory { get; set; }
    public bool IncludeSubdirectories { get; set; }
    public int BackupCount { get; set; }
    public List<BackupInfo>? Backups { get; set; }
}
