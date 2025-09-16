using System.Collections.Generic;

namespace FFMpeg.MCP.Host.Models.Output;

public class GenerateEqualChaptersResponse
{
    public string? Message { get; set; }
    public string[]? OutputFiles { get; set; }
    public int ChaptersGenerated { get; set; }
    public double ChapterDurationMinutes { get; set; }
    public double TotalDurationMinutes { get; set; }
    public List<object>? Chapters { get; set; }
}
