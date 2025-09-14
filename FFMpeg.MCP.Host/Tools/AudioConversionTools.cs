using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using FFMpeg.MCP.Host.Models;
using FFMpeg.MCP.Host.Services;
using System.ComponentModel;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tools;

[McpServerToolType]
public class AudioConversionTools
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<AudioConversionTools> _logger;

    public AudioConversionTools(IFFmpegService ffmpegService, ILogger<AudioConversionTools> logger)
    {
        _ffmpegService = ffmpegService;
        _logger = logger;
    }

    [McpServerTool, Description("Convert audio file to a different format")]
    public async Task<string> ConvertAudioFormatAsync(
        [Description("Full path to the input audio file")] string inputPath,
        [Description("Full path for the output file")] string outputPath,
        [Description("Target format (e.g., mp3, wav, flac, aac, ogg)")] string format,
        [Description("Audio bitrate in kbps (optional)")] int? bitrate = null,
        [Description("Audio codec (optional, will use default for format if not specified)")] string? codec = null)
    {
        try
        {
            var options = new ConversionOptions
            {
                OutputFormat = format,
                Bitrate = bitrate,
                Codec = codec
            };

            var result = await _ffmpegService.ConvertFileAsync(inputPath, outputPath, options);

            if (result.Success)
            {
                var inputInfo = await _ffmpegService.GetFileInfoAsync(inputPath);
                var outputInfo = await _ffmpegService.GetFileInfoAsync(outputPath);

                var response = new
                {
                    success = true,
                    message = result.Message,
                    outputFiles = result.OutputFiles,
                    conversion = new
                    {
                        inputFormat = inputInfo?.Format,
                        outputFormat = format,
                        inputSize = inputInfo?.FileSizeBytes,
                        outputSize = outputInfo?.FileSizeBytes,
                        inputDuration = inputInfo?.Duration.ToString(@"hh\:mm\:ss"),
                        outputDuration = outputInfo?.Duration.ToString(@"hh\:mm\:ss"),
                        bitrate = bitrate,
                        codec = codec
                    }
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return JsonSerializer.Serialize(new { success = false, message = result.Message, errorDetails = result.ErrorDetails }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting audio from {InputPath} to {OutputPath}", inputPath, outputPath);
            return JsonSerializer.Serialize(new { success = false, message = $"Error converting audio: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Convert audio to MP3 format with quality presets")]
    public async Task<string> ConvertToMp3Async(
        [Description("Full path to the input audio file")] string inputPath,
        [Description("Full path for the output MP3 file")] string outputPath,
        [Description("Quality preset: high (320k), medium (192k), low (128k)")] string quality = "medium")
    {
        try
        {
            var bitrate = quality.ToLower() switch
            {
                "high" => 320,
                "medium" => 192,
                "low" => 128,
                _ => 192
            };

            return await ConvertAudioFormatAsync(inputPath, outputPath, "mp3", bitrate, "libmp3lame");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting to MP3 for {InputPath}", inputPath);
            return JsonSerializer.Serialize(new { success = false, message = $"Error converting to MP3: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Convert audio to FLAC format (lossless compression)")]
    public async Task<string> ConvertToFlacAsync(
        [Description("Full path to the input audio file")] string inputPath,
        [Description("Full path for the output FLAC file")] string outputPath,
        [Description("Compression level (0-12, higher is better compression but slower)")] int compressionLevel = 5)
    {
        try
        {
            var options = new ConversionOptions
            {
                OutputFormat = "flac",
                CustomOptions = new Dictionary<string, string>
                {
                    ["-compression_level"] = compressionLevel.ToString()
                }
            };

            var result = await _ffmpegService.ConvertFileAsync(inputPath, outputPath, options);

            if (result.Success)
            {
                var inputInfo = await _ffmpegService.GetFileInfoAsync(inputPath);
                var outputInfo = await _ffmpegService.GetFileInfoAsync(outputPath);

                var response = new
                {
                    success = true,
                    message = result.Message,
                    outputFiles = result.OutputFiles,
                    conversion = new
                    {
                        format = "FLAC (lossless)",
                        compressionLevel = compressionLevel,
                        inputSize = inputInfo?.FileSizeBytes,
                        outputSize = outputInfo?.FileSizeBytes,
                        compressionRatio = inputInfo?.FileSizeBytes > 0 ?
                            $"{((double)(outputInfo?.FileSizeBytes ?? 0) / inputInfo.FileSizeBytes):P1}" : "N/A"
                    }
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return JsonSerializer.Serialize(new { success = false, message = result.Message, errorDetails = result.ErrorDetails }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting to FLAC for {InputPath}", inputPath);
            return JsonSerializer.Serialize(new { success = false, message = $"Error converting to FLAC: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Convert audio to WAV format (uncompressed)")]
    public async Task<string> ConvertToWavAsync(
        [Description("Full path to the input audio file")] string inputPath,
        [Description("Full path for the output WAV file")] string outputPath,
        [Description("Sample rate in Hz (optional, e.g., 44100, 48000)")] int? sampleRate = null,
        [Description("Bit depth (16, 24, 32)")] int bitDepth = 16)
    {
        try
        {
            var options = new ConversionOptions
            {
                OutputFormat = "wav",
                CustomOptions = new Dictionary<string, string>()
            };

            if (sampleRate.HasValue)
            {
                options.CustomOptions["-ar"] = sampleRate.Value.ToString();
            }

            options.CustomOptions["-sample_fmt"] = bitDepth switch
            {
                16 => "s16",
                24 => "s32", // FFmpeg doesn't have native s24, use s32
                32 => "s32",
                _ => "s16"
            };

            var result = await _ffmpegService.ConvertFileAsync(inputPath, outputPath, options);

            if (result.Success)
            {
                var response = new
                {
                    success = true,
                    message = result.Message,
                    outputFiles = result.OutputFiles,
                    conversion = new
                    {
                        format = "WAV (uncompressed)",
                        sampleRate = sampleRate,
                        bitDepth = bitDepth
                    }
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return JsonSerializer.Serialize(new { success = false, message = result.Message, errorDetails = result.ErrorDetails }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting to WAV for {InputPath}", inputPath);
            return JsonSerializer.Serialize(new { success = false, message = $"Error converting to WAV: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Convert audio to AAC format")]
    public async Task<string> ConvertToAacAsync(
        [Description("Full path to the input audio file")] string inputPath,
        [Description("Full path for the output AAC file")] string outputPath,
        [Description("Audio bitrate in kbps")] int bitrate = 128,
        [Description("Use high-efficiency AAC (HE-AAC) for lower bitrates")] bool useHEAAC = false)
    {
        try
        {
            var codec = useHEAAC ? "libfdk_aac" : "aac";
            var options = new ConversionOptions
            {
                OutputFormat = "adts", // AAC container format
                Bitrate = bitrate,
                Codec = codec
            };

            if (useHEAAC)
            {
                options.CustomOptions["-profile:a"] = "aac_he";
            }

            var result = await _ffmpegService.ConvertFileAsync(inputPath, outputPath, options);

            if (result.Success)
            {
                var response = new
                {
                    success = true,
                    message = result.Message,
                    outputFiles = result.OutputFiles,
                    conversion = new
                    {
                        format = useHEAAC ? "HE-AAC" : "AAC",
                        bitrate = bitrate,
                        codec = codec
                    }
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return JsonSerializer.Serialize(new { success = false, message = result.Message, errorDetails = result.ErrorDetails }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting to AAC for {InputPath}", inputPath);
            return JsonSerializer.Serialize(new { success = false, message = $"Error converting to AAC: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Convert audio with advanced custom options")]
    public async Task<string> ConvertAudioAdvancedAsync(
        [Description("Full path to the input audio file")] string inputPath,
        [Description("Full path for the output file")] string outputPath,
        [Description("JSON object with conversion options")] string optionsJson)
    {
        try
        {
            var optionsData = JsonSerializer.Deserialize<Dictionary<string, object>>(optionsJson);
            if (optionsData == null)
                return JsonSerializer.Serialize(new { success = false, message = "Invalid options JSON provided" });

            var options = new ConversionOptions();

            if (optionsData.ContainsKey("format"))
            {
                options.OutputFormat = optionsData["format"].ToString();
            }

            if (optionsData.ContainsKey("bitrate"))
            {
                if (int.TryParse(optionsData["bitrate"].ToString(), out var bitrate))
                {
                    options.Bitrate = bitrate;
                }
            }

            if (optionsData.ContainsKey("codec"))
            {
                options.Codec = optionsData["codec"].ToString();
            }

            if (optionsData.ContainsKey("quality"))
            {
                if (int.TryParse(optionsData["quality"].ToString(), out var quality))
                {
                    options.Quality = quality;
                }
            }

            if (optionsData.ContainsKey("customOptions"))
            {
                var customOptionsJson = optionsData["customOptions"].ToString();
                if (!string.IsNullOrEmpty(customOptionsJson))
                {
                    var customOptions = JsonSerializer.Deserialize<Dictionary<string, string>>(customOptionsJson);
                    if (customOptions != null)
                    {
                        options.CustomOptions = customOptions;
                    }
                }
            }

            var result = await _ffmpegService.ConvertFileAsync(inputPath, outputPath, options);

            if (result.Success)
            {
                var response = new
                {
                    success = true,
                    message = result.Message,
                    outputFiles = result.OutputFiles,
                    conversion = new
                    {
                        format = options.OutputFormat,
                        bitrate = options.Bitrate,
                        codec = options.Codec,
                        quality = options.Quality,
                        customOptions = options.CustomOptions
                    }
                };
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            return JsonSerializer.Serialize(new { success = false, message = result.Message, errorDetails = result.ErrorDetails }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting audio with advanced options from {InputPath} to {OutputPath}", inputPath, outputPath);
            return JsonSerializer.Serialize(new { success = false, message = $"Error converting audio: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Batch convert multiple audio files to the same format")]
    public async Task<string> BatchConvertAudioAsync(
        [Description("JSON array of input file paths")] string inputPathsJson,
        [Description("Output directory path")] string outputDirectory,
        [Description("Target format (e.g., mp3, wav, flac)")] string format,
        [Description("Audio bitrate in kbps (optional)")] int? bitrate = null,
        [Description("Keep original filenames (true) or generate new ones (false)")] bool keepOriginalNames = true)
    {
        try
        {
            var inputPaths = JsonSerializer.Deserialize<string[]>(inputPathsJson);
            if (inputPaths == null || !inputPaths.Any())
                return JsonSerializer.Serialize(new { success = false, message = "Invalid or empty input paths JSON provided" });

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var results = new List<object>();
            var successCount = 0;
            var failureCount = 0;

            foreach (var inputPath in inputPaths)
            {
                try
                {
                    var fileName = keepOriginalNames
                        ? Path.GetFileNameWithoutExtension(inputPath)
                        : $"converted_{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileNameWithoutExtension(inputPath)}";

                    var outputPath = Path.Combine(outputDirectory, $"{fileName}.{format}");

                    var options = new ConversionOptions
                    {
                        OutputFormat = format,
                        Bitrate = bitrate
                    };

                    var result = await _ffmpegService.ConvertFileAsync(inputPath, outputPath, options);

                    results.Add(new
                    {
                        inputPath = inputPath,
                        outputPath = outputPath,
                        success = result.Success,
                        message = result.Message
                    });

                    if (result.Success)
                        successCount++;
                    else
                        failureCount++;
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        inputPath = inputPath,
                        success = false,
                        message = $"Error: {ex.Message}"
                    });
                    failureCount++;
                }
            }

            var response = new
            {
                success = successCount > 0,
                message = $"Batch conversion completed: {successCount} successful, {failureCount} failed",
                totalFiles = inputPaths.Length,
                successCount = successCount,
                failureCount = failureCount,
                results = results
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch conversion to {OutputDirectory}", outputDirectory);
            return JsonSerializer.Serialize(new { success = false, message = $"Error in batch conversion: {ex.Message}" });
        }
    }
}