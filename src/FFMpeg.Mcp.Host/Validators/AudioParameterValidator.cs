namespace FFMpeg.Mcp.Host.Validators;

public static class AudioParameterValidator
{
    private static readonly HashSet<string> ValidFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp3", "wav", "aac", "flac", "ogg", "m4a"
    };

    private static readonly HashSet<int> ValidSampleRates = new()
    {
        22050, 44100, 48000, 96000
    };

    public static void ValidateFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Audio format cannot be null or empty.", nameof(format));
        }

        if (!ValidFormats.Contains(format))
        {
            throw new ArgumentException($"Unsupported audio format: {format}. Supported formats: {string.Join(", ", ValidFormats)}", nameof(format));
        }
    }

    public static void ValidateBitrate(int? bitrate)
    {
        if (bitrate.HasValue)
        {
            if (bitrate.Value < 64 || bitrate.Value > 320)
            {
                throw new ArgumentException("Bitrate must be between 64 and 320 kbps.", nameof(bitrate));
            }
        }
    }

    public static void ValidateSampleRate(int? sampleRate)
    {
        if (sampleRate.HasValue)
        {
            if (!ValidSampleRates.Contains(sampleRate.Value))
            {
                throw new ArgumentException($"Unsupported sample rate: {sampleRate}. Supported sample rates: {string.Join(", ", ValidSampleRates)}", nameof(sampleRate));
            }
        }
    }

    public static void ValidateTimestamps(TimeSpan start, TimeSpan end, TimeSpan duration)
    {
        if (start < TimeSpan.Zero)
        {
            throw new ArgumentException("Start time cannot be negative.", nameof(start));
        }

        if (end < TimeSpan.Zero)
        {
            throw new ArgumentException("End time cannot be negative.", nameof(end));
        }

        if (start >= end)
        {
            throw new ArgumentException("Start time must be less than end time.", nameof(start));
        }

        if (start > duration)
        {
            throw new ArgumentException("Start time cannot be greater than the duration.", nameof(start));
        }

        if (end > duration)
        {
            throw new ArgumentException("End time cannot be greater than the duration.", nameof(end));
        }
    }

    public static void ValidateChannelCount(int channels)
    {
        if (channels <= 0)
        {
            throw new ArgumentException("Channel count must be greater than zero.", nameof(channels));
        }
    }
}