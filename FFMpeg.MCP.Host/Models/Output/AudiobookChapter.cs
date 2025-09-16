namespace FFMpeg.MCP.Host.Models.Output;

public class AudiobookChapter
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
}
