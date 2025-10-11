namespace FFMpeg.Mcp.Host.Validators;

public static class FilePathValidator
{
    public static void ValidateInputPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Input path cannot be null or empty.", nameof(path));
        }

        if (!Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("Input path must be an absolute path.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new ArgumentException($"Input file does not exist: {path}", nameof(path));
        }

        try
        {
            using var stream = File.OpenRead(path);
        }
        catch (UnauthorizedAccessException)
        {
            throw new ArgumentException($"Input file is not readable: {path}", nameof(path));
        }
        catch (IOException ex)
        {
            throw new ArgumentException($"Cannot read input file: {path}. {ex.Message}", nameof(path));
        }
    }

    public static void ValidateOutputPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Output path cannot be null or empty.", nameof(path));
        }

        if (!Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("Output path must be an absolute path.", nameof(path));
        }

        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory))
        {
            throw new ArgumentException("Cannot determine directory from output path.", nameof(path));
        }

        if (!Directory.Exists(directory))
        {
            throw new ArgumentException($"Output directory does not exist: {directory}", nameof(path));
        }

        try
        {
            var testFile = Path.Combine(directory, $"test_write_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (UnauthorizedAccessException)
        {
            throw new ArgumentException($"Output directory is not writable: {directory}", nameof(path));
        }
        catch (IOException ex)
        {
            throw new ArgumentException($"Cannot write to output directory: {directory}. {ex.Message}", nameof(path));
        }
    }

    public static bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            var fileName = Path.GetFileName(path);

            return !string.IsNullOrEmpty(fileName) &&
                   fileName.IndexOfAny(Path.GetInvalidFileNameChars()) == -1 &&
                   path.IndexOfAny(Path.GetInvalidPathChars()) == -1;
        }
        catch
        {
            return false;
        }
    }
}