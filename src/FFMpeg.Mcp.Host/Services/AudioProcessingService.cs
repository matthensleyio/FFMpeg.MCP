using FFMpegCore;
using FFMpeg.Mcp.Host.Models;
using FFMpeg.Mcp.Host.Validators;

namespace FFMpeg.Mcp.Host.Services;

public class AudioProcessingService : IAudioProcessingService
{
    public async Task<AudioFileInfo> GetAudioInfoAsync(string filePath, CancellationToken cancellationToken)
    {
        FilePathValidator.ValidateInputPath(filePath);

        var mediaInfo = await FFProbe.AnalyseAsync(filePath, null, cancellationToken);
        var audioStream = mediaInfo.PrimaryAudioStream ?? throw new InvalidOperationException("No audio stream found in file");

        return new AudioFileInfo
        {
            FilePath = filePath,
            Format = Path.GetExtension(filePath).TrimStart('.'),
            Duration = mediaInfo.Duration,
            Codec = audioStream.CodecName,
            SampleRate = audioStream.SampleRateHz,
            Bitrate = audioStream.BitRate,
            Channels = audioStream.Channels,
            FileSize = new FileInfo(filePath).Length
        };
    }

    public async Task<OperationResult> ConvertAudioAsync(
        string inputPath,
        string outputPath,
        string format,
        int? bitrate,
        int? sampleRate,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var operationId = Guid.NewGuid();

        try
        {
            FilePathValidator.ValidateInputPath(inputPath);
            FilePathValidator.ValidateOutputPath(outputPath);
            AudioParameterValidator.ValidateFormat(format);

            if (bitrate.HasValue)
                AudioParameterValidator.ValidateBitrate(bitrate.Value);

            if (sampleRate.HasValue)
                AudioParameterValidator.ValidateSampleRate(sampleRate.Value);

            var mediaInfo = await FFProbe.AnalyseAsync(inputPath, null, cancellationToken);
            var arguments = FFMpegArguments.FromFileInput(inputPath);

            await arguments.OutputToFile(outputPath, true, options =>
            {
                options.WithAudioCodec(GetCodecForFormat(format));

                if (bitrate.HasValue)
                    options.WithAudioBitrate(bitrate.Value);

                if (sampleRate.HasValue)
                    options.WithCustomArgument($"-ar {sampleRate.Value}");
            })
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

            var endTime = DateTime.UtcNow;
            var outputFileInfo = new FileInfo(outputPath);

            return new OperationResult
            {
                OperationId = operationId,
                Success = true,
                OutputFilePath = outputPath,
                ErrorMessage = null,
                ProcessingTime = endTime - startTime,
                OutputFileSize = outputFileInfo.Exists ? outputFileInfo.Length : 0
            };
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            return new OperationResult
            {
                OperationId = operationId,
                Success = false,
                OutputFilePath = null,
                ErrorMessage = ex.Message,
                ProcessingTime = endTime - startTime,
                OutputFileSize = 0
            };
        }
    }

    public async Task<OperationResult> TrimAudioAsync(
        string inputPath,
        string outputPath,
        TimeSpan start,
        TimeSpan end,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var operationId = Guid.NewGuid();

        try
        {
            FilePathValidator.ValidateInputPath(inputPath);
            FilePathValidator.ValidateOutputPath(outputPath);

            var mediaInfo = await FFProbe.AnalyseAsync(inputPath, null, cancellationToken);
            AudioParameterValidator.ValidateTimestamps(start, end, mediaInfo.Duration);
            var duration = end - start;

            var arguments = FFMpegArguments.FromFileInput(inputPath, false, options =>
            {
                options.Seek(start);
            });

            await arguments.OutputToFile(outputPath, true, options =>
            {
                options.WithDuration(duration);
                options.CopyChannel();
            })
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

            var endTimeStamp = DateTime.UtcNow;
            var outputFileInfo = new FileInfo(outputPath);

            return new OperationResult
            {
                OperationId = operationId,
                Success = true,
                OutputFilePath = outputPath,
                ErrorMessage = null,
                ProcessingTime = endTimeStamp - startTime,
                OutputFileSize = outputFileInfo.Exists ? outputFileInfo.Length : 0
            };
        }
        catch (Exception ex)
        {
            var endTimeStamp = DateTime.UtcNow;
            return new OperationResult
            {
                OperationId = operationId,
                Success = false,
                OutputFilePath = null,
                ErrorMessage = ex.Message,
                ProcessingTime = endTimeStamp - startTime,
                OutputFileSize = 0
            };
        }
    }

    public async Task<OperationResult> AdjustQualityAsync(
        string inputPath,
        string outputPath,
        int? bitrate,
        int? sampleRate,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var operationId = Guid.NewGuid();

        try
        {
            FilePathValidator.ValidateInputPath(inputPath);
            FilePathValidator.ValidateOutputPath(outputPath);

            if (bitrate.HasValue)
                AudioParameterValidator.ValidateBitrate(bitrate.Value);

            if (sampleRate.HasValue)
                AudioParameterValidator.ValidateSampleRate(sampleRate.Value);

            var arguments = FFMpegArguments.FromFileInput(inputPath);

            await arguments.OutputToFile(outputPath, true, options =>
            {
                if (bitrate.HasValue)
                    options.WithAudioBitrate(bitrate.Value);

                if (sampleRate.HasValue)
                    options.WithCustomArgument($"-ar {sampleRate.Value}");
            })
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

            var endTime = DateTime.UtcNow;
            var outputFileInfo = new FileInfo(outputPath);

            return new OperationResult
            {
                OperationId = operationId,
                Success = true,
                OutputFilePath = outputPath,
                ErrorMessage = null,
                ProcessingTime = endTime - startTime,
                OutputFileSize = outputFileInfo.Exists ? outputFileInfo.Length : 0
            };
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            return new OperationResult
            {
                OperationId = operationId,
                Success = false,
                OutputFilePath = null,
                ErrorMessage = ex.Message,
                ProcessingTime = endTime - startTime,
                OutputFileSize = 0
            };
        }
    }

    public async Task<OperationResult> ExtractChannelAsync(
        string inputPath,
        string outputPath,
        string channel,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var operationId = Guid.NewGuid();

        try
        {
            FilePathValidator.ValidateInputPath(inputPath);
            FilePathValidator.ValidateOutputPath(outputPath);

            if (!int.TryParse(channel, out int channelIndex) || channelIndex < 0)
                throw new ArgumentException("Channel must be a non-negative integer", nameof(channel));

            var mediaInfo = await FFProbe.AnalyseAsync(inputPath, null, cancellationToken);
            var audioStream = mediaInfo.PrimaryAudioStream ?? throw new InvalidOperationException("No audio stream found in file");

            if (channelIndex >= audioStream.Channels)
                throw new ArgumentException($"Channel index {channelIndex} is out of range. Audio has {audioStream.Channels} channels.", nameof(channel));

            var arguments = FFMpegArguments.FromFileInput(inputPath);

            await arguments.OutputToFile(outputPath, true, options =>
            {
                options.WithCustomArgument($"-map_channel 0.{audioStream.Index}.{channelIndex}");
            })
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

            var endTime = DateTime.UtcNow;
            var outputFileInfo = new FileInfo(outputPath);

            return new OperationResult
            {
                OperationId = operationId,
                Success = true,
                OutputFilePath = outputPath,
                ErrorMessage = null,
                ProcessingTime = endTime - startTime,
                OutputFileSize = outputFileInfo.Exists ? outputFileInfo.Length : 0
            };
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            return new OperationResult
            {
                OperationId = operationId,
                Success = false,
                OutputFilePath = null,
                ErrorMessage = ex.Message,
                ProcessingTime = endTime - startTime,
                OutputFileSize = 0
            };
        }
    }

    public async Task<OperationResult> MergeAudioAsync(
        List<string> inputPaths,
        string outputPath,
        string outputFormat,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var operationId = Guid.NewGuid();

        try
        {
            if (inputPaths == null || inputPaths.Count < 2)
                throw new ArgumentException("At least two input files are required for merging", nameof(inputPaths));

            foreach (var inputPath in inputPaths)
                FilePathValidator.ValidateInputPath(inputPath);

            FilePathValidator.ValidateOutputPath(outputPath);
            AudioParameterValidator.ValidateFormat(outputFormat);

            var arguments = FFMpegArguments.FromFileInput(inputPaths[0]);

            for (int i = 1; i < inputPaths.Count; i++)
            {
                arguments = arguments.AddFileInput(inputPaths[i]);
            }

            await arguments.OutputToFile(outputPath, true, options =>
            {
                options.WithCustomArgument($"-filter_complex concat=n={inputPaths.Count}:v=0:a=1");
                options.WithAudioCodec(GetCodecForFormat(outputFormat));
            })
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

            var endTime = DateTime.UtcNow;
            var outputFileInfo = new FileInfo(outputPath);

            return new OperationResult
            {
                OperationId = operationId,
                Success = true,
                OutputFilePath = outputPath,
                ErrorMessage = null,
                ProcessingTime = endTime - startTime,
                OutputFileSize = outputFileInfo.Exists ? outputFileInfo.Length : 0
            };
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            return new OperationResult
            {
                OperationId = operationId,
                Success = false,
                OutputFilePath = null,
                ErrorMessage = ex.Message,
                ProcessingTime = endTime - startTime,
                OutputFileSize = 0
            };
        }
    }

    private static string GetCodecForFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "mp3" => "libmp3lame",
            "aac" or "m4a" => "aac",
            "wav" => "pcm_s16le",
            "flac" => "flac",
            "ogg" => "libvorbis",
            _ => throw new ArgumentException($"Unsupported audio format: {format}", nameof(format))
        };
    }
}
