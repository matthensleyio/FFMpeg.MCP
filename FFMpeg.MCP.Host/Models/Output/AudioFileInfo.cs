using System.Collections.Generic;
using FFMpeg.MCP.Host.Models.Output;

namespace FFMpeg.MCP.Host.Models.Output;

public class AudioFileInfo
{
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public System.TimeSpan Duration { get; set; }
    public string? Format { get; set; }
    public long FileSizeBytes { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<ChapterInfo> Chapters { get; set; } = new();
}
