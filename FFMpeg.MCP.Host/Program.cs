using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Reflection;
using System.IO;
using Serilog;
using System.Diagnostics;
using FFMpeg.MCP.Host.Services;
using FFMpeg.MCP.Host.Mcp;

namespace FFMpeg.MCP.Host
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.SetOut(Console.Error);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    path: "logs/ffmpeg-mcp.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 3,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    shared: true)
                .CreateLogger();

            PrintAvailableTools();

            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

            builder.Services.AddSerilog();

            builder.Services
                .AddHttpClient()
                .AddScoped<IFFmpegService, FFmpegService>()
                .AddScoped<McpDispatcher>()
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

            await builder.Build().RunAsync();
        }

        private static void PrintAvailableTools()
        {
            Console.Error.WriteLine("=== Available FFmpeg MCP Tools ===");
            Console.Error.WriteLine();

            var assembly = Assembly.GetExecutingAssembly();
            var toolTypes = assembly.GetTypes()
                .Where(type => type.GetCustomAttribute<McpServerToolTypeAttribute>() != null)
                .OrderBy(type => type.Name);

            foreach (var toolType in toolTypes)
            {
                Console.Error.WriteLine($"[{toolType.Name}]");

                var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() != null)
                    .OrderBy(method => method.Name);

                foreach (var method in methods)
                {
                    var descriptionAttr = method.GetCustomAttribute<DescriptionAttribute>();
                    var description = descriptionAttr?.Description ?? "No description available";

                    Console.Error.WriteLine($"  - {method.Name}: {description}");
                }

                Console.Error.WriteLine();
            }

            Console.Error.WriteLine("=== End of Available Tools ===");
            Console.Error.WriteLine();
        }
    }
}
