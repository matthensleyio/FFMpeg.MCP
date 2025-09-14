# FFMpeg MCP Host Integration Tests

This test project provides comprehensive integration tests for all FFMpeg MCP server functionality. The tests are designed to work with real audio files and perform actual FFMpeg operations to ensure the MCP tools work correctly end-to-end.

## Test Setup Requirements

### 1. Test Audio Files

Place the following test audio files in the `test-audio-files/` directory:

#### Required Files:
- **`test-short.mp3`** - 1-2 minutes, music or speech, with basic metadata
- **`test-speech.wav`** - 3-5 minutes, clear speech recording, uncompressed WAV
- **`test-long.mp3`** - 15-30 minutes, longer content for duration-based splitting
- **`test-chapters.m4a`** - Audio file with existing chapter markers (if available)

#### Optional Batch Test Files:
Create a `batch-test/` subdirectory with 3-5 small audio files for batch processing tests.

### 2. File Specifications

For best test coverage, use files with these characteristics:

```
test-short.mp3:
  - Duration: 1-2 minutes
  - Format: MP3, 192kbps
  - Metadata: Title, Artist, Album tags present

test-speech.wav:
  - Duration: 3-5 minutes
  - Format: WAV, 44.1kHz, 16-bit
  - Content: Clear speech (good for silence detection tests)

test-long.mp3:
  - Duration: 15-30 minutes
  - Format: MP3
  - Content: Podcast/audiobook style content

test-chapters.m4a:
  - Duration: 10+ minutes
  - Format: M4A/AAC
  - Chapters: 3-5 chapter markers if possible
```

## Running Tests

### Prerequisites

1. **FFMpeg Installation**: Ensure FFMpeg is installed and available in system PATH
2. **Test Files**: Place required audio files in `test-audio-files/` directory
3. **Disk Space**: Tests create temporary files, ensure adequate space

### Command Line

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "ClassName=AudioMetadataToolsTests"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run tests in parallel
dotnet test --parallel
```

### Visual Studio

1. Open Test Explorer (Test → Windows → Test Explorer)
2. Build solution to discover tests
3. Run individual tests or test groups
4. View detailed test output in Test Detail Summary

## Test Structure

### Test Classes

1. **`AudioMetadataToolsTests`** - Tests metadata operations
   - File info retrieval
   - Metadata updates (common fields and custom JSON)
   - Format support queries
   - FFMpeg availability checks

2. **`AudioSplittingToolsTests`** - Tests audio splitting functionality
   - Duration-based splitting (seconds and minutes)
   - Chapter-based splitting
   - Advanced splitting with custom options
   - Chapter information extraction

3. **`AudioConversionToolsTests`** - Tests format conversion
   - Format conversions (MP3↔WAV↔FLAC↔AAC)
   - Quality presets and custom settings
   - Batch conversion operations
   - Advanced conversion with custom parameters

4. **`AudioChapterToolsTests`** - Tests chapter management
   - Setting custom chapters
   - Generating equal-time chapters
   - Chapter export (JSON, CSV, TXT, CUE)
   - Chapter removal operations

5. **`AudioBackupToolsTests`** - Tests backup operations
   - Single file backups
   - Batch backup operations
   - Archive creation (ZIP with metadata)
   - Backup restoration and cleanup

6. **`IntegrationWorkflowTests`** - Tests complete workflows
   - Full audiobook processing pipeline
   - Cross-format conversion workflows
   - Batch processing scenarios
   - Error handling and recovery

### Test Base Class

**`TestBase`** provides common functionality:
- **File Management**: Automatically copies test files and tracks created files
- **Cleanup**: Removes all created files and directories after each test
- **Utilities**: Helper methods for file operations and JSON deserialization
- **Assertions**: Custom assertions for file existence and properties

## Test File Management

### Automatic Cleanup
- Tests never modify original files in `test-audio-files/`
- All operations work on copies in temporary directories
- **Automatic cleanup** removes all generated files after each test
- Each test runs in isolated temporary directory

### File Tracking
Tests automatically track:
- Copied input files
- Generated output files
- Created directories
- Backup files
- Archive files

### Manual Cleanup
If tests are interrupted, temporary files may remain in:
- `%TEMP%/ffmpeg-tests-*` directories

Clean these manually if needed:
```bash
# Windows
rmdir /s "%TEMP%\ffmpeg-tests-*"

# Linux/Mac
rm -rf /tmp/ffmpeg-tests-*
```

## Expected Test Behavior

### Successful Tests
- ✅ All operations complete without exceptions
- ✅ Output files are created with expected properties
- ✅ JSON responses have correct structure and data
- ✅ File sizes and formats are reasonable
- ✅ Cleanup removes all temporary files

### Conditional Tests
Some tests may pass conditionally:
- **Chapter operations**: May skip if test files lack chapters
- **Format conversions**: May fail if FFMpeg codecs unavailable
- **Silence detection**: Currently returns "not implemented" message

### Error Tests
Error condition tests verify:
- ❌ Non-existent files return appropriate error messages
- ❌ Invalid JSON input is handled gracefully
- ❌ Unsupported operations return meaningful errors
- ❌ No exceptions are thrown for invalid input

## Troubleshooting

### Common Issues

1. **FFMpeg Not Found**
   ```
   Error: FFMpeg is not available
   Solution: Install FFMpeg and add to system PATH
   ```

2. **Missing Test Files**
   ```
   FileNotFoundException: Test file not found
   Solution: Add required files to test-audio-files/ directory
   ```

3. **Permission Errors**
   ```
   UnauthorizedAccessException
   Solution: Run tests with appropriate file system permissions
   ```

4. **Disk Space Issues**
   ```
   Tests may fail if insufficient disk space
   Solution: Ensure adequate free space for temporary files
   ```

### Debug Information

Tests include detailed logging:
- File operations and paths
- FFMpeg command execution details
- JSON response content
- Error messages and stack traces

Enable verbose logging:
```bash
dotnet test --logger "console;verbosity=diagnostic"
```

## Test Coverage

### Tool Categories Tested
- ✅ **27 MCP Tools** across 5 tool categories
- ✅ **All major workflows** (audiobook processing, format conversion)
- ✅ **Error conditions** and edge cases
- ✅ **File management** and cleanup
- ✅ **Cross-tool integration** scenarios

### Operations Tested
- ✅ **File Analysis**: Metadata extraction, format detection
- ✅ **Format Conversion**: All supported format combinations
- ✅ **Audio Splitting**: Duration and chapter-based splitting
- ✅ **Chapter Management**: Creation, editing, export, removal
- ✅ **Backup Operations**: Single, batch, archive, restore
- ✅ **Batch Processing**: Multiple file operations
- ✅ **Error Handling**: Invalid inputs and edge cases

The test suite provides comprehensive validation of all FFMpeg MCP functionality with realistic audio processing scenarios.