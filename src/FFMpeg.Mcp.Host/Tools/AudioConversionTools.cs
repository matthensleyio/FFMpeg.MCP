using System.ComponentModel;
using FFMpeg.Mcp.Host.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace FFMpeg.Mcp.Host.Tools;

[McpServerToolType]
public class AudioConversionTools
{
    [McpServerTool(Name = "convert_audio"), Description("Convert audio file from one format to another")]
    public static async Task<string> ConvertAudio(
        IAudioProcessingService audioService,
        McpServer server,
        RequestContext<CallToolRequestParams> context,
        [Description("Path to the input audio file")] string inputPath,
        [Description("Path for the output audio file")] string outputPath,
        [Description("Target audio format (mp3, wav, aac, flac, ogg, m4a)")] string targetFormat,
        [Description("Optional target bitrate in kbps (64-320)")] int? bitrate = null,
        [Description("Optional sample rate in Hz (22050, 44100, 48000, 96000)")] int? sampleRate = null)
    {
        var progressToken = context.Params?.ProgressToken;

        if (progressToken is not null)
        {
            await server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams
            {
                ProgressToken = progressToken.Value,
                Progress = new ProgressNotificationValue
                {
                    Progress = 0,
                    Total = 100,
                    Message = "Starting audio conversion...",
                },
            });
        }

        var result = await audioService.ConvertAudioAsync(
            inputPath,
            outputPath,
            targetFormat,
            bitrate,
            sampleRate,
            CancellationToken.None);

        if (progressToken is not null)
        {
            await server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams
            {
                ProgressToken = progressToken.Value,
                Progress = new ProgressNotificationValue
                {
                    Progress = 100,
                    Total = 100,
                    Message = "Audio conversion completed.",
                },
            });
        }

        if (result.Success)
        {
            return $"Successfully converted audio to {outputPath}. Processing time: {result.ProcessingTime.TotalSeconds:F2}s. Output size: {result.OutputFileSize / 1024.0:F2} KB";
        }
        else
        {
            return $"Failed to convert audio: {result.ErrorMessage}";
        }
    }

    [McpServerTool(Name = "get_audio_info"), Description("Get detailed information about an audio file")]
    public static async Task<string> GetAudioInfo(
        IAudioProcessingService audioService,
        [Description("Path to the audio file")] string filePath)
    {
        var info = await audioService.GetAudioInfoAsync(filePath, CancellationToken.None);

        return $"File: {info.FilePath}\n" +
               $"Format: {info.Format}\n" +
               $"Duration: {info.Duration.TotalSeconds:F2}s\n" +
               $"Codec: {info.Codec}\n" +
               $"Sample Rate: {info.SampleRate} Hz\n" +
               $"Bitrate: {info.Bitrate / 1000.0:F0} kbps\n" +
               $"Channels: {info.Channels}\n" +
               $"File Size: {info.FileSize / 1024.0:F2} KB";
    }

    [McpServerTool(Name = "trim_audio"), Description("Trim/cut audio file to a specific time range")]
    public static async Task<string> TrimAudio(
        IAudioProcessingService audioService,
        McpServer server,
        RequestContext<CallToolRequestParams> context,
        [Description("Path to the input audio file")] string inputPath,
        [Description("Path for the output audio file")] string outputPath,
        [Description("Start time in seconds")] double startSeconds,
        [Description("End time in seconds")] double endSeconds)
    {
        var progressToken = context.Params?.ProgressToken;
        var startTime = TimeSpan.FromSeconds(startSeconds);
        var endTime = TimeSpan.FromSeconds(endSeconds);

        if (progressToken is not null)
        {
            await server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams
            {
                ProgressToken = progressToken.Value,
                Progress = new ProgressNotificationValue
                {
                    Progress = 0,
                    Total = 100,
                    Message = "Starting audio trim operation...",
                },
            });
        }

        var result = await audioService.TrimAudioAsync(
            inputPath,
            outputPath,
            startTime,
            endTime,
            CancellationToken.None);

        if (progressToken is not null)
        {
            await server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams
            {
                ProgressToken = progressToken.Value,
                Progress = new ProgressNotificationValue
                {
                    Progress = 100,
                    Total = 100,
                    Message = "Audio trim completed.",
                },
            });
        }

        if (result.Success)
        {
            return $"Successfully trimmed audio to {outputPath}. Processing time: {result.ProcessingTime.TotalSeconds:F2}s. Output size: {result.OutputFileSize / 1024.0:F2} KB";
        }
        else
        {
            return $"Failed to trim audio: {result.ErrorMessage}";
        }
    }

    [McpServerTool(Name = "adjust_audio_quality"), Description("Adjust audio quality by changing bitrate and/or sample rate")]
    public static async Task<string> AdjustAudioQuality(
        IAudioProcessingService audioService,
        McpServer server,
        RequestContext<CallToolRequestParams> context,
        [Description("Path to the input audio file")] string inputPath,
        [Description("Path for the output audio file")] string outputPath,
        [Description("Optional target bitrate in kbps (64-320)")] int? bitrate = null,
        [Description("Optional sample rate in Hz (22050, 44100, 48000, 96000)")] int? sampleRate = null)
    {
        var progressToken = context.Params?.ProgressToken;

        if (progressToken is not null)
        {
            await server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams
            {
                ProgressToken = progressToken.Value,
                Progress = new ProgressNotificationValue
                {
                    Progress = 0,
                    Total = 100,
                    Message = "Starting audio quality adjustment...",
                },
            });
        }

        var result = await audioService.AdjustQualityAsync(
            inputPath,
            outputPath,
            bitrate,
            sampleRate,
            CancellationToken.None);

        if (progressToken is not null)
        {
            await server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams
            {
                ProgressToken = progressToken.Value,
                Progress = new ProgressNotificationValue
                {
                    Progress = 100,
                    Total = 100,
                    Message = "Quality adjustment completed.",
                },
            });
        }

        if (result.Success)
        {
            return $"Successfully adjusted audio quality to {outputPath}. Processing time: {result.ProcessingTime.TotalSeconds:F2}s. Output size: {result.OutputFileSize / 1024.0:F2} KB";
        }
        else
        {
            return $"Failed to adjust audio quality: {result.ErrorMessage}";
        }
    }

    [McpServerTool(Name = "extract_audio_channel"), Description("Extract a specific channel from an audio file")]
    public static async Task<string> ExtractAudioChannel(
        IAudioProcessingService audioService,
        McpServer server,
        RequestContext<CallToolRequestParams> context,
        [Description("Path to the input audio file")] string inputPath,
        [Description("Path for the output audio file")] string outputPath,
        [Description("Channel index to extract (0 for left, 1 for right in stereo)")] string channel)
    {
        var progressToken = context.Params?.ProgressToken;

        if (progressToken is not null)
        {
            await server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams
            {
                ProgressToken = progressToken.Value,
                Progress = new ProgressNotificationValue
                {
                    Progress = 0,
                    Total = 100,
                    Message = "Starting channel extraction...",
                },
            });
        }

        var result = await audioService.ExtractChannelAsync(
            inputPath,
            outputPath,
            channel,
            CancellationToken.None);

        if (progressToken is not null)
        {
            await server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams
            {
                ProgressToken = progressToken.Value,
                Progress = new ProgressNotificationValue
                {
                    Progress = 100,
                    Total = 100,
                    Message = "Channel extraction completed.",
                },
            });
        }

        if (result.Success)
        {
            return $"Successfully extracted channel {channel} to {outputPath}. Processing time: {result.ProcessingTime.TotalSeconds:F2}s. Output size: {result.OutputFileSize / 1024.0:F2} KB";
        }
        else
        {
            return $"Failed to extract channel: {result.ErrorMessage}";
        }
    }

    [McpServerTool(Name = "merge_audio_files"), Description("Merge multiple audio files into a single file")]
    public static async Task<string> MergeAudioFiles(
        IAudioProcessingService audioService,
        McpServer server,
        RequestContext<CallToolRequestParams> context,
        [Description("Comma-separated paths to input audio files")] string inputPathsCsv,
        [Description("Path for the output audio file")] string outputPath,
        [Description("Output format (mp3, wav, aac, flac, ogg, m4a)")] string outputFormat)
    {
        var inputPaths = inputPathsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var progressToken = context.Params?.ProgressToken;

        if (progressToken is not null)
        {
            await server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams
            {
                ProgressToken = progressToken.Value,
                Progress = new ProgressNotificationValue
                {
                    Progress = 0,
                    Total = 100,
                    Message = $"Starting merge of {inputPaths.Count} audio files...",
                },
            });
        }

        var result = await audioService.MergeAudioAsync(
            inputPaths,
            outputPath,
            outputFormat,
            CancellationToken.None);

        if (progressToken is not null)
        {
            await server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams
            {
                ProgressToken = progressToken.Value,
                Progress = new ProgressNotificationValue
                {
                    Progress = 100,
                    Total = 100,
                    Message = "Audio merge completed.",
                },
            });
        }

        if (result.Success)
        {
            return $"Successfully merged {inputPaths.Count} audio files to {outputPath}. Processing time: {result.ProcessingTime.TotalSeconds:F2}s. Output size: {result.OutputFileSize / 1024.0:F2} KB";
        }
        else
        {
            return $"Failed to merge audio files: {result.ErrorMessage}";
        }
    }
}
