using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class GetChaptersResponse
{
    public string? FilePath { get; set; }
    public bool HasChapters { get; set; }
    public int ChapterCount { get; set; }
    public List<ChapterInfo>? Chapters { get; set; }
}
