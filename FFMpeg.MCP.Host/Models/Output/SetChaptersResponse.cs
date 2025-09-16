using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class SetChaptersResponse
{
    public string? Message { get; set; }
    public string[]? OutputFiles { get; set; }
    public int ChaptersAdded { get; set; }
    public List<ChapterInfoResponse>? Chapters { get; set; }
}
