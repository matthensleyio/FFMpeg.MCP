using FFMpegMcpServer.Models;

namespace FFMpegMcpServer.Services;

public interface IAudioProcessingService
{
    Task<AudioFileInfo> GetAudioInfoAsync(string filePath, CancellationToken cancellationToken);

    Task<OperationResult> ConvertAudioAsync(string inputPath, string outputPath, string format, int? bitrate, int? sampleRate, CancellationToken cancellationToken);

    Task<OperationResult> TrimAudioAsync(string inputPath, string outputPath, TimeSpan start, TimeSpan end, CancellationToken cancellationToken);

    Task<OperationResult> AdjustQualityAsync(string inputPath, string outputPath, int? bitrate, int? sampleRate, CancellationToken cancellationToken);

    Task<OperationResult> ExtractChannelAsync(string inputPath, string outputPath, string channel, CancellationToken cancellationToken);

    Task<OperationResult> MergeAudioAsync(List<string> inputPaths, string outputPath, string outputFormat, CancellationToken cancellationToken);
}