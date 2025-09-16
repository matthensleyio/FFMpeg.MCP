using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class CleanupBackupsResponse
{
    public string? Message { get; set; }
    public string? Directory { get; set; }
    public bool DryRun { get; set; }
    public int? MaxAgeDays { get; set; }
    public int? MaxBackupsPerFile { get; set; }
    public int TotalBackupFiles { get; set; }
    public int FilesToDelete { get; set; }
    public List<string>? DeletedFiles { get; set; }
    public long SpaceSavedBytes { get; set; }
    public double SpaceSavedMB { get; set; }
}
