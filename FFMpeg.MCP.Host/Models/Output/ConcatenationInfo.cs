using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class ConcatenationInfo
{
    public int InputFileCount { get; set; }
    public string? OutputFormat { get; set; }
    public long? TotalInputSize { get; set; }
    public long? OutputSize { get; set; }
    public string? TotalDuration { get; set; }
    public int ChapterCount { get; set; }
    public List<AudiobookChapter>? Chapters { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
