namespace FFMpeg.MCP.Host.Models.Output;

public class ExportChapterInfoResponse
{
    public string? Message { get; set; }
    public string? OutputFile { get; set; }
    public int ChaptersExported { get; set; }
    public string? Format { get; set; }
}
