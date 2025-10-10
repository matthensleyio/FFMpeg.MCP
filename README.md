# FFMpeg MCP Server

A Model Context Protocol (MCP) server that exposes FFMpeg audio processing functionality to AI clients.

## Features

This MCP server provides 6 audio processing tools:

1. **convert_audio** - Convert audio files between formats (mp3, wav, aac, flac, ogg, m4a)
2. **get_audio_info** - Get detailed information about audio files
3. **trim_audio** - Trim/cut audio to specific time ranges
4. **adjust_audio_quality** - Adjust bitrate and sample rate
5. **extract_audio_channel** - Extract specific channels from multi-channel audio
6. **merge_audio_files** - Merge multiple audio files into one

## Requirements

- .NET 9.0 SDK
- FFMpeg installed and available in system PATH

## Installation

### Install FFMpeg

#### Windows
```bash
winget install FFmpeg
```

#### macOS
```bash
brew install ffmpeg
```

#### Linux
```bash
sudo apt-get install ffmpeg
```

### Build the Server

```bash
dotnet build src/FFMpegMcpServer/FFMpegMcpServer.csproj
```

## Usage

### Running the Server

The server uses stdio transport for communication with MCP clients:

```bash
dotnet run --project src/FFMpegMcpServer/FFMpegMcpServer.csproj
```

### Configuring with Claude Desktop

Add this configuration to your Claude Desktop config file:

**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "ffmpeg": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\path\\to\\FFMpeg.MCP\\src\\FFMpegMcpServer\\FFMpegMcpServer.csproj"
      ]
    }
  }
}
```

## Available Tools

### convert_audio

Convert audio from one format to another.

**Parameters:**
- `inputPath` (string, required) - Path to input audio file
- `outputPath` (string, required) - Path for output audio file
- `targetFormat` (string, required) - Target format: mp3, wav, aac, flac, ogg, m4a
- `bitrate` (int, optional) - Target bitrate in kbps (64-320)
- `sampleRate` (int, optional) - Sample rate in Hz (22050, 44100, 48000, 96000)

**Example:**
```
Convert my-audio.wav to MP3 at 192 kbps
```

### get_audio_info

Get detailed metadata about an audio file.

**Parameters:**
- `filePath` (string, required) - Path to audio file

**Example:**
```
Get info about my-audio.mp3
```

### trim_audio

Trim audio to a specific time range.

**Parameters:**
- `inputPath` (string, required) - Path to input audio file
- `outputPath` (string, required) - Path for output audio file
- `startSeconds` (double, required) - Start time in seconds
- `endSeconds` (double, required) - End time in seconds

**Example:**
```
Trim my-audio.wav from 10 seconds to 30 seconds
```

### adjust_audio_quality

Adjust audio quality by changing bitrate and/or sample rate.

**Parameters:**
- `inputPath` (string, required) - Path to input audio file
- `outputPath` (string, required) - Path for output audio file
- `bitrate` (int, optional) - Target bitrate in kbps (64-320)
- `sampleRate` (int, optional) - Sample rate in Hz (22050, 44100, 48000, 96000)

**Example:**
```
Reduce quality of my-audio.mp3 to 128 kbps
```

### extract_audio_channel

Extract a specific channel from multi-channel audio.

**Parameters:**
- `inputPath` (string, required) - Path to input audio file
- `outputPath` (string, required) - Path for output audio file
- `channel` (string, required) - Channel index (0 for left, 1 for right in stereo)

**Example:**
```
Extract left channel from stereo-audio.wav
```

### merge_audio_files

Merge multiple audio files into one.

**Parameters:**
- `inputPathsCsv` (string, required) - Comma-separated paths to input files
- `outputPath` (string, required) - Path for output audio file
- `outputFormat` (string, required) - Output format: mp3, wav, aac, flac, ogg, m4a

**Example:**
```
Merge file1.mp3, file2.mp3, and file3.mp3 into combined.mp3
```

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test tests/FFMpegMcpServer.UnitTests/FFMpegMcpServer.UnitTests.csproj

# Run only integration tests
dotnet test tests/FFMpegMcpServer.IntegrationTests/FFMpegMcpServer.IntegrationTests.csproj
```

### Project Structure

```
FFMpeg.MCP/
├── src/
│   └── FFMpegMcpServer/
│       ├── Models/          # Data models
│       ├── Services/        # Core audio processing services
│       ├── Tools/           # MCP tool definitions
│       ├── Validators/      # Input validation
│       └── Program.cs       # Entry point
├── tests/
│   ├── FFMpegMcpServer.UnitTests/
│   └── FFMpegMcpServer.IntegrationTests/
└── README.md
```

## Architecture

- **MCP Server**: Uses ModelContextProtocol library for stdio transport
- **Audio Processing**: FFMpegCore library wraps FFMpeg functionality
- **Background Processing**: Channel-based task queue for async operations
- **Progress Notifications**: Real-time progress updates for long-running operations
- **Dependency Injection**: Microsoft.Extensions.Hosting for service management

## Supported Audio Formats

- MP3 (.mp3)
- WAV (.wav)
- AAC (.aac, .m4a)
- FLAC (.flac)
- OGG (.ogg)

## Error Handling

All operations return detailed error messages when failures occur. Common error scenarios:

- Invalid file paths
- Unsupported audio formats
- Invalid parameter values (bitrate, sample rate, etc.)
- FFMpeg processing errors

## License

This project uses the following open-source libraries:
- FFMpegCore (MIT License)
- ModelContextProtocol (MIT License)
- Microsoft.Extensions.Hosting (MIT License)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests to ensure everything passes
5. Submit a pull request

## Support

For issues and questions:
- Check FFMpeg installation: `ffmpeg -version`
- Verify .NET version: `dotnet --version`
- Review server logs for error messages
