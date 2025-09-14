using System.ComponentModel;

namespace FFMpeg.MCP.Host.Models;

public class AudioFileInfo
{
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Format { get; set; }
    public long FileSizeBytes { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<ChapterInfo> Chapters { get; set; } = new();
}

public class ChapterInfo
{
    public int Index { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class ConversionOptions
{
    public string? OutputFormat { get; set; }
    public int? Quality { get; set; }
    public int? Bitrate { get; set; }
    public string? Codec { get; set; }
    public Dictionary<string, string> CustomOptions { get; set; } = new();
}

public class SplitOptions
{
    public TimeSpan? MaxDuration { get; set; }
    public bool SplitByChapters { get; set; }
    public string? OutputPattern { get; set; }
    public bool PreserveMetadata { get; set; } = true;
}

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> OutputFiles { get; set; } = new();
    public string? ErrorDetails { get; set; }
}