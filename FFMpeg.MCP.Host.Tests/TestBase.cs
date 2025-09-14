using FFMpeg.MCP.Host.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Text.Json;

namespace FFMpeg.MCP.Host.Tests;

public abstract class TestBase : IDisposable
{
    protected readonly IFFmpegService FFmpegService;
    protected readonly string TestAudioDirectory;
    protected readonly string WorkingDirectory;
    protected readonly List<string> CreatedFiles = new();
    protected readonly List<string> CreatedDirectories = new();

    protected TestBase()
    {
        // Setup test directories
        var testProjectDir = Path.GetDirectoryName(typeof(TestBase).Assembly.Location) ?? throw new InvalidOperationException("Cannot determine test directory");
        TestAudioDirectory = Path.Combine(testProjectDir, "test-audio-files");
        WorkingDirectory = Path.Combine(Path.GetTempPath(), $"ffmpeg-tests-{Guid.NewGuid():N}");

        Directory.CreateDirectory(WorkingDirectory);
        CreatedDirectories.Add(WorkingDirectory);

        // Setup logging for tests
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog());
        services.AddScoped<IFFmpegService, FFmpegService>();

        var serviceProvider = services.BuildServiceProvider();
        FFmpegService = serviceProvider.GetRequiredService<IFFmpegService>();
    }

    protected string CopyTestFile(string testFileName)
    {
        var sourcePath = Path.Combine(TestAudioDirectory, testFileName);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Test file not found: {sourcePath}. Please ensure test audio files are present.");
        }

        var destinationPath = Path.Combine(WorkingDirectory, testFileName);
        File.Copy(sourcePath, destinationPath);
        CreatedFiles.Add(destinationPath);
        return destinationPath;
    }

    protected string GetWorkingPath(string fileName)
    {
        var path = Path.Combine(WorkingDirectory, fileName);
        CreatedFiles.Add(path);
        return path;
    }

    protected void TrackCreatedFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            CreatedFiles.Add(filePath);
        }
    }

    protected void TrackCreatedDirectory(string dirPath)
    {
        if (Directory.Exists(dirPath))
        {
            CreatedDirectories.Add(dirPath);
        }
    }

    protected Task<T> DeserializeResponse<T>(string jsonResponse)
    {
        var result = JsonSerializer.Deserialize<T>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return Task.FromResult(result ?? throw new InvalidOperationException($"Failed to deserialize response to {typeof(T).Name}"));
    }

    protected void AssertFileExists(string filePath, string message = "")
    {
        Assert.True(File.Exists(filePath), $"File should exist: {filePath}. {message}");
    }

    protected void AssertFileDoesNotExist(string filePath, string message = "")
    {
        Assert.False(File.Exists(filePath), $"File should not exist: {filePath}. {message}");
    }

    protected long GetFileSize(string filePath)
    {
        return new FileInfo(filePath).Length;
    }

    public void Dispose()
    {
        // Clean up all created files
        foreach (var file in CreatedFiles.ToList())
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete file {file}: {ex.Message}");
            }
        }

        // Clean up created directories (in reverse order)
        foreach (var dir in CreatedDirectories.AsEnumerable().Reverse().ToList())
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete directory {dir}: {ex.Message}");
            }
        }

        CreatedFiles.Clear();
        CreatedDirectories.Clear();
    }
}