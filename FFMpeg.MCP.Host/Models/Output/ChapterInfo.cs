using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class ChapterInfo
{
    public int Index { get; set; }
    public System.TimeSpan StartTime { get; set; }
    public System.TimeSpan EndTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
