using System.Globalization;
using System.Text.Json;

namespace AudioConverter.Infrastructure.Remix;

public static class LoudnessParser
{
    public static LoudnessMeasurements Parse(string output)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(output);
        var marker = output.IndexOf("\"input_i\"", StringComparison.Ordinal);
        if (marker < 0) throw new InvalidDataException("FFmpeg did not return loudness measurements.");
        var start = output.LastIndexOf('{', marker);
        var end = output.IndexOf('}', marker);
        if (start < 0 || end < start) throw new InvalidDataException("Invalid FFmpeg loudness response.");

        using var document = JsonDocument.Parse(output[start..(end + 1)]);
        var root = document.RootElement;
        return new LoudnessMeasurements(
            Read(root, "input_i"),
            Read(root, "input_tp"),
            Read(root, "input_lra"),
            Read(root, "input_thresh"),
            Read(root, "target_offset"));
    }

    private static double Read(JsonElement root, string name)
    {
        var text = root.GetProperty(name).GetString();
        if (text?.Equals("-inf", StringComparison.OrdinalIgnoreCase) == true) return double.NegativeInfinity;
        if (text?.Equals("inf", StringComparison.OrdinalIgnoreCase) == true || text?.Equals("+inf", StringComparison.OrdinalIgnoreCase) == true) return double.PositiveInfinity;
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidDataException($"Invalid loudness value: {name}");
    }
}
