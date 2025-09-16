using System;

namespace FFMpeg.MCP.Host.Models.Input;

public class SplitOptions
{
    public TimeSpan? MaxDuration { get; set; }
    public bool SplitByChapters { get; set; }
    public string? OutputPattern { get; set; }
    public bool PreserveMetadata { get; set; } = true;
}
