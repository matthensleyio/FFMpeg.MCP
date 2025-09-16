# FFMpeg MCP Server

A Model Context Protocol (MCP) server that provides tools to interact with the powerful FFMpeg multimedia framework. This server exposes various FFMpeg functionalities as MCP tools that can be used by MCP-compatible clients for a wide range of audio and image processing tasks.

## Features

This MCP server provides tools for:

### General
- `check_ffmpeg_availability` - Check if FFmpeg is installed and accessible.
- `get_supported_formats` - List all audio formats supported by the installed FFmpeg.

### Audio Conversion
- `convert_audio_format` - Convert an audio file to a different format (e.g., mp3, wav, flac).
- `batch_convert_audio` - Convert multiple audio files to the same format in one go.

### Audio Splitting & Chapters
- `split_audio_by_chapters` - Split an audio file into multiple files based on its chapter markers.
- `split_audio_by_duration` - Split an audio file into segments of a specific duration.
- `get_audio_chapters` - Retrieve a list of chapters from an audio file.
- `get_operation_progress` - Check the progress of long-running splitting operations.

### Chapter Management
- `set_audio_chapters` - Add or update chapters in an audio file using a JSON structure.
- `generate_equal_chapters` - Automatically generate chapters of equal duration.
- `remove_chapters` - Remove all chapter markers from an audio file.
- `export_chapter_info` - Export chapter information to a file (JSON, CSV).

### Metadata
- `get_audio_file_info` - Get comprehensive metadata and technical details about an audio file.
- `update_audio_metadata` - Update an audio file's metadata from a JSON object.
- `update_common_metadata` - A helper to easily update common tags like title, artist, album, etc.

### Audiobook Creation
- `concatenate_to_audiobook` - Combine multiple audio files into a single M4B audiobook with chapters and metadata.

### Backup & Restore
- `create_audio_backup` - Create a timestamped backup of a single audio file.
- `create_batch_backup` - Create backups for a list of audio files.
- `create_archive_backup` - Create a single compressed ZIP archive of multiple audio files.
- `restore_from_backup` - Restore an audio file from a backup copy.
- `list_backups` - List all available backups in a specified directory.
- `cleanup_backups` - Clean up old backups based on age or number of copies.

### Image Conversion
- `convert_image_to_ico` - Convert an image file (e.g., PNG, JPG) to an ICO file.

## Setup

### 1. Install FFmpeg
This server requires FFmpeg to be installed and available in your system's PATH.
- **Windows**: For a simplified setup, you can use the included PowerShell script. Open a PowerShell terminal and run:
  ```powershell
  ./FFMpeg.MCP.Host/Assets/ffmpeg/ffmpeg-install.ps1
  ```
  This script will download the latest essential build of FFmpeg, extract it, and automatically add it to your user PATH. Alternatively, you can download from the [official website](https://www.ffmpeg.org/download.html) and add the `bin` directory to your PATH manually.
- **macOS**: Install using Homebrew: `brew install ffmpeg`
- **Linux**: Install using your package manager, e.g., `sudo apt-get install ffmpeg`

You can verify the installation by running `ffmpeg -version` in your terminal.

### 2. Build and Run the Server
This project is built with .NET.

```sh
# Restore dependencies and build the project
dotnet build

# Run the server
dotnet run --project FFMpeg.MCP.Host/FFMpeg.MCP.Host.csproj
```
The server will start and listen for connections from an MCP client.

## Claude Desktop Integration

To use this MCP server with Claude Desktop, add it to your Claude Desktop configuration file.

### Configuration File Location
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`

### Configuration Example: Using `dotnet`

Add the following to your `claude_desktop_config.json`:
```json
{
  "mcpServers": {
    "ffmpeg": {
      "command": "dotnet",
      "args": [
        "run",
        "--project", "C:\\path\\to\\your\\FFMpeg.MCP.Host\\FFMpeg.MCP.Host.csproj"
      ],
      "icon": "file://C:\\path\\to\\your\\FFMpeg.MCP.Host\\Assets\\ffmpeg-logo.png"
    }
  }
}
```

### Configuration Example: Using a Published Executable

If you prefer to use a compiled executable, first publish the project:
```sh
dotnet publish -c Release -r win-x64 --self-contained true
```
*(Replace `win-x64` with your target platform, e.g., `osx-arm64` for Apple Silicon)*

Then, use the following configuration:
```json
{
  "mcpServers": {
    "ffmpeg": {
      "command": "C:\\path\\to\\your\\FFMpeg.MCP.Host\\bin\\Release\\net8.0\\win-x64\\publish\\FFMpeg.MCP.Host.exe",
      "args": [],
      "icon": "file://C:\\path\\to\\your\\FFMpeg.MCP.Host\\Assets\\ffmpeg-logo.png"
    }
  }
}
```

### Setup Steps
1.  Replace `C:\\path\\to\\your\\` with the **absolute path** to the project on your machine.
2.  Save the configuration file.
3.  Restart Claude Desktop.

Once configured, you'll see the FFMpeg MCP server connected in Claude Desktop, and you'll be able to use all the FFMpeg tools directly in your conversations!

## Dependencies
- .NET 8.0
- FFmpeg
- ModelContextProtocol
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Http
- Serilog
