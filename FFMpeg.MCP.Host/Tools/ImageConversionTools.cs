using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using FFMpeg.MCP.Host.Models;
using FFMpeg.MCP.Host.Services;
using System.ComponentModel;
using FFMpeg.MCP.Host.Mcp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FFMpeg.MCP.Host.Tools;

#region Response Models
public class ImageConversionResponse
{
    public string? Message { get; set; }
    public List<string>? OutputFiles { get; set; }
    public ImageConversionInfo? Conversion { get; set; }
}

public class ImageConversionInfo
{
    public string? InputFormat { get; set; }
    public string? OutputFormat { get; set; }
    public long? InputSize { get; set; }
    public long? OutputSize { get; set; }
    public string? InputDimensions { get; set; }
    public string? OutputDimensions { get; set; }
    public int? IconSize { get; set; }
}
#endregion

[McpServerToolType]
public class ImageConversionTools
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<ImageConversionTools> _logger;
    private readonly McpDispatcher _dispatcher;

    public ImageConversionTools(IFFmpegService ffmpegService, ILogger<ImageConversionTools> logger, McpDispatcher dispatcher)
    {
        _ffmpegService = ffmpegService;
        _logger = logger;
        _dispatcher = dispatcher;
    }

    [McpServerTool, Description("Convert an image file to .ico format")]
    public Task<McpResponse<ImageConversionResponse>> ConvertImageToIcoAsync(
        [Description("Full path to the input image file (PNG, JPG, BMP, etc.)")] string inputPath,
        [Description("Full path for the output .ico file")] string outputPath,
        [Description("Icon size in pixels (16, 32, 48, 64, 128, 256). Default is 32.")] int? iconSize = 32)
    {
        return _dispatcher.DispatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(inputPath)) throw new ArgumentException("Input path is required.", nameof(inputPath));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path is required.", nameof(outputPath));
            if (!File.Exists(inputPath)) throw new FileNotFoundException("Input image file not found.", inputPath);

            // Validate icon size
            var validSizes = new[] { 16, 32, 48, 64, 128, 256 };
            if (iconSize.HasValue && !Array.Exists(validSizes, size => size == iconSize.Value))
            {
                throw new ArgumentException($"Invalid icon size. Valid sizes are: {string.Join(", ", validSizes)}", nameof(iconSize));
            }

            // Ensure output path has .ico extension
            if (!outputPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
            {
                outputPath = Path.ChangeExtension(outputPath, ".ico");
            }

            // Create output directory if it doesn't exist
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Get input file info
            var inputFileInfo = new FileInfo(inputPath);
            var inputSize = inputFileInfo.Length;

            // Use custom conversion options for image to ico conversion
            var options = new ConversionOptions
            {
                OutputFormat = "ico",
                CustomOptions = new Dictionary<string, string>()
            };

            // Add size filter if specified
            if (iconSize.HasValue)
            {
                options.CustomOptions.Add("-vf", $"scale={iconSize.Value}:{iconSize.Value}");
            }

            var result = await _ffmpegService.ConvertFileAsync(inputPath, outputPath, options);

            if (!result.Success) throw new InvalidOperationException($"Failed to convert image to .ico: {result.Message}");

            // Get output file info
            var outputFileInfo = new FileInfo(outputPath);
            var outputSize = outputFileInfo.Exists ? outputFileInfo.Length : 0;

            return new ImageConversionResponse
            {
                Message = result.Message,
                OutputFiles = result.OutputFiles,
                Conversion = new ImageConversionInfo
                {
                    InputFormat = Path.GetExtension(inputPath).TrimStart('.').ToLowerInvariant(),
                    OutputFormat = "ico",
                    InputSize = inputSize,
                    OutputSize = outputSize,
                    IconSize = iconSize
                }
            };
        });
    }
}