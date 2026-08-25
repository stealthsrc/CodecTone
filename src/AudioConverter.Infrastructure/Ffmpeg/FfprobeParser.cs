using System.Globalization;
using System.Text.Json;
using AudioConverter.Core.Models;

namespace AudioConverter.Infrastructure.Ffmpeg;

public static class FfprobeParser
{
    public static ProbeInfo Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var streams = root.GetProperty("streams");
        JsonElement? audio = null;
        var hasCover = false;
        foreach (var stream in streams.EnumerateArray())
        {
            if (stream.TryGetProperty("codec_type", out var type) && type.GetString() == "audio" && audio is null)
            {
                audio = stream;
            }
            if (stream.TryGetProperty("disposition", out var disposition)
                && disposition.TryGetProperty("attached_pic", out var attached)
                && attached.GetInt32() == 1)
            {
                hasCover = true;
            }
        }
        if (audio is null)
        {
            throw new InvalidDataException("The selected file does not contain an audio stream.");
        }

        var format = root.GetProperty("format");
        var duration = ReadDouble(format, "duration") ?? 0;
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (format.TryGetProperty("tags", out var tagElement))
        {
            foreach (var tag in tagElement.EnumerateObject())
            {
                tags[tag.Name] = tag.Value.ToString();
            }
        }

        return new ProbeInfo(
            duration,
            ReadString(audio.Value, "codec_name") ?? "unknown",
            ReadInt(audio.Value, "sample_rate"),
            ReadInt(audio.Value, "bits_per_raw_sample") ?? ReadInt(audio.Value, "bits_per_sample"),
            hasCover,
            tags,
            ReadLong(format, "size"),
            ToKbps(ReadLong(audio.Value, "bit_rate")),
            ToKbps(ReadLong(format, "bit_rate")),
            ReadInt(audio.Value, "channels"));
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value.ToString() : null;

    private static int? ReadInt(JsonElement element, string name) =>
        int.TryParse(ReadString(element, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static double? ReadDouble(JsonElement element, string name) =>
        double.TryParse(ReadString(element, name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static long? ReadLong(JsonElement element, string name) =>
        long.TryParse(ReadString(element, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static int? ToKbps(long? bitsPerSecond) => bitsPerSecond is null
        ? null
        : (int)(bitsPerSecond.Value / 1000);
}
