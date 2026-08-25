using System.Globalization;

namespace AudioConverter.Infrastructure.Ffmpeg;

public static class FfmpegProgressParser
{
    public static double? Parse(string line, double durationSeconds)
    {
        if (line == "progress=end")
        {
            return 1;
        }
        if (durationSeconds <= 0 || (!line.StartsWith("out_time_us=") && !line.StartsWith("out_time_ms=")))
        {
            return null;
        }
        var separator = line.IndexOf('=');
        if (!long.TryParse(line[(separator + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var microseconds))
        {
            return null;
        }
        return Math.Clamp(microseconds / 1_000_000d / durationSeconds, 0, 0.99);
    }
}
