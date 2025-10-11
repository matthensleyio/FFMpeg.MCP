# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FFMpeg MCP Server - A Model Context Protocol (MCP) server that exposes FFMpeg audio processing functionality to AI clients. Provides 6 audio tools via stdio transport.

## Requirements

- .NET 9.0 SDK
- FFMpeg must be installed and available in system PATH

## Build and Test Commands

```bash
# Build the server
dotnet build src/FFMpeg.Mcp.Host/FFMpeg.Mcp.Host.csproj

# Build entire solution
dotnet build FFMpeg.Mcp.sln

# Run the MCP server (uses stdio transport)
dotnet run --project src/FFMpeg.Mcp.Host/FFMpeg.Mcp.Host.csproj

# Run all tests
dotnet test

# Run only unit tests
dotnet test tests/FFMpeg.Mcp.Host.UnitTests/FFMpeg.Mcp.Host.UnitTests.csproj

# Run only integration tests
dotnet test tests/FFMpeg.Mcp.Host.IntegrationTests/FFMpeg.Mcp.Host.IntegrationTests.csproj
```

## Architecture

### MCP Server Pattern
- **Entry point**: Program.cs configures services and MCP server with stdio transport
- **Tool discovery**: Uses `WithToolsFromAssembly()` to automatically register all tools marked with `[McpServerToolType]`
- **Transport**: Stdio-based communication via ModelContextProtocol library

### Core Services
- **AudioProcessingService**: Main service wrapping FFMpegCore library for audio operations
- **BackgroundTaskQueue**: Channel-based bounded queue (capacity: 100) for async task processing
- **QueuedHostedService**: Background service that processes queued tasks

### Tool Implementation Pattern
All tools are defined in `AudioConversionTools.cs` using MCP attributes:
- Tools are static async methods decorated with `[McpServerTool]`
- Tool parameters use `[Description]` attributes for documentation
- Tools receive injected dependencies: `IAudioProcessingService`, `McpServer`, `RequestContext`
- Progress notifications sent via `server.SendNotificationAsync("notifications/progress", ...)` when `progressToken` is provided

### Dependency Injection Setup
Services registered in Program.cs:
```csharp
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddSingleton<IAudioProcessingService, AudioProcessingService>();
```

### Channel-based Task Processing
- Uses `System.Threading.Channels` for thread-safe work queue
- BoundedChannel configured with FullMode.Wait (blocks when queue is full)
- Supports multiple readers and writers

### Validation Pattern
- **FilePathValidator**: Validates input/output file paths
- **AudioParameterValidator**: Validates formats, bitrates, sample rates, timestamps
- Validators throw ArgumentException with descriptive messages on failure

### Audio Format Codec Mapping
Located in `AudioProcessingService.GetCodecForFormat()`:
- mp3 → libmp3lame
- aac/m4a → aac
- wav → pcm_s16le
- flac → flac
- ogg → libvorbis

### Operation Result Pattern
All audio operations return `OperationResult` containing:
- OperationId (Guid)
- Success (bool)
- OutputFilePath
- ErrorMessage
- ProcessingTime (TimeSpan)
- OutputFileSize (long)

## Available Tools

1. **convert_audio** - Format conversion with optional bitrate/sample rate adjustment
2. **get_audio_info** - Extract metadata using FFProbe
3. **trim_audio** - Time-based audio trimming
4. **adjust_audio_quality** - Bitrate/sample rate modification
5. **extract_audio_channel** - Channel extraction from multi-channel audio
6. **merge_audio_files** - Concatenate multiple audio files

## Key Dependencies

- **FFMpegCore** (5.2.0): FFMpeg wrapper for .NET
- **ModelContextProtocol** (0.4.0-preview.2): MCP server implementation
- **Microsoft.Extensions.Hosting** (9.0.9): Dependency injection and hosted services

## Important Notes

- All FFMpeg operations use `CancellableThrough(cancellationToken)` for graceful cancellation
- Temporary files stored in system temp directory (configured in Program.cs via `GlobalFFOptions`)
- All tool methods use `CancellationToken.None` when calling service methods (consider passing real tokens in future)
- BackgroundTaskQueue has fixed capacity of 100 items - queue operations will block when full
