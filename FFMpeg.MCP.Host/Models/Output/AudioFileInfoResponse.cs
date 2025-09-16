namespace FFMpeg.MCP.Host.Models.Output;

public class AudioFileInfoResponse
{
    public string? FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string? Duration { get; set; }
    public string? Format { get; set; }
}
