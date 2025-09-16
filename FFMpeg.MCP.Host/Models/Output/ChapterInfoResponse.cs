namespace FFMpeg.MCP.Host.Models.Output;

public class ChapterInfoResponse
{
    public int Index { get; set; }
    public string? Title { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Duration { get; set; }
}
