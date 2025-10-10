using FFMpegCore;
using FFMpegMcpServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;

GlobalFFOptions.Configure(options =>
{
    options.TemporaryFilesFolder = Path.GetTempPath();
    options.WorkingDirectory = Environment.CurrentDirectory;
});

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddSingleton<IAudioProcessingService, AudioProcessingService>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var host = builder.Build();
await host.RunAsync();
