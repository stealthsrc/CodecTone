using System.Globalization;

namespace AudioConverter.Infrastructure.Remix;

public sealed record RemixDynamics(double PeakDb, double CrestFactorDb);
public sealed record RemixSpectral(double LowEnergyDb, double MidEnergyDb, double HighEnergyDb, double StereoWidth);

public static class RemixAnalysisParser
{
    public static RemixDynamics ParseDynamics(string output)
    {
        var peak = ReadLastMetric(output, "Peak level dB:");
        var rms = ReadLastMetric(output, "RMS level dB:");
        var crest = peak - rms;
        return new RemixDynamics(peak, double.IsFinite(crest) ? Math.Max(0, crest) : 0);
    }

    public static RemixSpectral ParseSpectral(string output)
    {
        var measurements = new SortedDictionary<int, double>();
        foreach (var line in output.Split('\n'))
        {
            if (!line.Contains("RMS level dB:", StringComparison.Ordinal)) continue;
            const string prefix = "[Parsed_astats_";
            var prefixStart = line.IndexOf(prefix, StringComparison.Ordinal);
            if (prefixStart < 0) continue;
            var idStart = prefixStart + prefix.Length;
            var bracketEnd = line.IndexOf(']', idStart);
            var spaceEnd = line.IndexOf(' ', idStart);
            var idEnd = spaceEnd >= 0 && (bracketEnd < 0 || spaceEnd < bracketEnd) ? spaceEnd : bracketEnd;
            if (idEnd < 0 || !int.TryParse(line[idStart..idEnd], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)) continue;
            measurements[id] = ParseValue(line);
        }

        var values = measurements.Values.ToArray();
        if (values.Length < 5) throw new InvalidDataException("FFmpeg did not return all spectral measurements.");
        var width = double.IsNegativeInfinity(values[4]) || double.IsNegativeInfinity(values[3])
            ? 0
            : Math.Clamp(Math.Pow(10, (values[4] - values[3]) / 20), 0, 1.5);
        return new RemixSpectral(values[0], values[1], values[2], width);
    }

    private static double ReadLastMetric(string output, string marker)
    {
        var line = output.Split('\n').LastOrDefault(value => value.Contains(marker, StringComparison.Ordinal));
        return line is null ? throw new InvalidDataException($"FFmpeg did not return {marker}") : ParseValue(line);
    }

    private static double ParseValue(string line)
    {
        var separator = line.LastIndexOf(':');
        if (separator < 0) throw new InvalidDataException("Invalid FFmpeg analysis value.");
        var text = line[(separator + 1)..].Trim();
        if (text.Equals("-inf", StringComparison.OrdinalIgnoreCase)) return double.NegativeInfinity;
        if (text.Equals("inf", StringComparison.OrdinalIgnoreCase) || text.Equals("+inf", StringComparison.OrdinalIgnoreCase)) return double.PositiveInfinity;
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new InvalidDataException("Invalid FFmpeg analysis value.");
        return value;
    }
}
