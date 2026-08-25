using System.Globalization;
using AudioConverter.Core.Models;
using AudioConverter.Core.Remix;
using AudioConverter.Core.Validation;

namespace AudioConverter.Infrastructure.Remix;

public static class RemixCommandBuilder
{
    public static string[] BuildPreview(
        string inputPath,
        string outputPath,
        IReadOnlyList<RemixEffect> effects,
        int sampleRate,
        double sourceDurationSeconds,
        double previewStartSeconds,
        double previewDurationSeconds)
    {
        var end = Math.Min(sourceDurationSeconds, previewStartSeconds + previewDurationSeconds);
        var rack = RemixFilterBuilder.Build(effects, sampleRate, sourceDurationSeconds, preview: true);
        var filters = $"atrim=start={Number(previewStartSeconds)}:end={Number(end)},asetpts=PTS-STARTPTS";
        if (!string.IsNullOrEmpty(rack)) filters += "," + rack;
        return
        [
            "-hide_banner", "-loglevel", "error", "-y", "-i", inputPath,
            "-map", "0:a:0", "-vn", "-af", filters,
            "-ac", "2", "-ar", "44100", "-c:a", "pcm_s16le",
            "-progress", "pipe:1", "-nostats", outputPath,
        ];
    }

    public static string[] BuildExport(
        string inputPath,
        string outputPath,
        IReadOnlyList<RemixEffect> effects,
        ConversionOptions encoding,
        RemixMetadata metadata,
        int sampleRate,
        double sourceDurationSeconds,
        bool sourceHasCover,
        LoudnessMeasurements? loudnessMeasurements = null)
    {
        OptionValidator.Validate(encoding);
        RemixMetadataValidator.Validate(metadata);
        var arguments = new List<string>
        {
            "-hide_banner", "-loglevel", "error", encoding.Overwrite ? "-y" : "-n",
            "-i", inputPath,
        };
        var replaceCover = metadata.CoverAction == CoverArtAction.Replace;
        if (replaceCover) arguments.AddRange(["-i", metadata.CoverPath!]);
        arguments.AddRange(["-map", "0:a:0"]);

        var supportsCover = encoding.OutputFormat is AudioFormat.Mp3 or AudioFormat.M4a or AudioFormat.Flac;
        var includeCover = supportsCover && metadata.CoverAction != CoverArtAction.Remove
            && (replaceCover || sourceHasCover);
        if (includeCover)
            arguments.AddRange(["-map", replaceCover ? "1:v:0" : "0:v:disp:attached_pic?"]);
        else
            arguments.Add("-vn");

        arguments.AddRange(encoding.PreserveMetadata
            ? ["-map_metadata", "0"]
            : ["-map_metadata", "-1"]);

        AddEncoding(arguments, encoding);
        var filter = RemixFilterBuilder.Build(
            effects,
            sampleRate,
            sourceDurationSeconds,
            preview: false,
            loudnessMeasurements);
        if (!string.IsNullOrEmpty(filter)) arguments.AddRange(["-af", filter]);
        AddMetadata(arguments, metadata);

        if (includeCover)
        {
            arguments.AddRange([
                "-c:v", replaceCover || encoding.OutputFormat != AudioFormat.Flac ? "mjpeg" : "copy",
                "-disposition:v:0", "attached_pic",
            ]);
        }
        arguments.AddRange(["-progress", "pipe:1", "-nostats", outputPath]);
        return [.. arguments];
    }

    private static void AddEncoding(List<string> arguments, ConversionOptions options)
    {
        switch (options.OutputFormat)
        {
            case AudioFormat.Mp3: arguments.AddRange(["-c:a", "libmp3lame", "-b:a", options.Bitrate ?? "192k"]); break;
            case AudioFormat.Aac: arguments.AddRange(["-c:a", "aac", "-b:a", options.Bitrate ?? "192k", "-f", "adts"]); break;
            case AudioFormat.Ogg: arguments.AddRange(["-c:a", "libvorbis", "-b:a", options.Bitrate ?? "192k"]); break;
            case AudioFormat.M4a: arguments.AddRange(["-c:a", "aac", "-b:a", options.Bitrate ?? "192k"]); break;
            case AudioFormat.Wav:
                arguments.AddRange(["-c:a", options.BitDepth switch { 24 => "pcm_s24le", 32 => "pcm_s32le", _ => "pcm_s16le" }]);
                if (options.SampleRate is not null) arguments.AddRange(["-ar", options.SampleRate.Value.ToString(CultureInfo.InvariantCulture)]);
                break;
            case AudioFormat.Flac:
                arguments.AddRange(["-c:a", "flac"]);
                if (options.SampleRate is not null) arguments.AddRange(["-ar", options.SampleRate.Value.ToString(CultureInfo.InvariantCulture)]);
                if (options.BitDepth == 16) arguments.AddRange(["-sample_fmt", "s16"]);
                else if (options.BitDepth == 24) arguments.AddRange(["-sample_fmt", "s32", "-bits_per_raw_sample", "24"]);
                break;
        }
    }

    private static void AddMetadata(List<string> arguments, RemixMetadata metadata)
    {
        var values = new Dictionary<string, string?>
        {
            ["title"] = metadata.Title,
            ["artist"] = metadata.Artist,
            ["album"] = metadata.Album,
            ["album_artist"] = metadata.AlbumArtist,
            ["genre"] = metadata.Genre,
            ["date"] = metadata.Date,
            ["track"] = metadata.Track,
            ["disc"] = metadata.Disc,
            ["comment"] = metadata.Comment,
        };
        foreach (var pair in values.Where(pair => pair.Value is not null))
            arguments.AddRange(["-metadata", $"{pair.Key}={pair.Value}"]);
        foreach (var pair in metadata.CustomTags ?? new Dictionary<string, string>())
            arguments.AddRange(["-metadata", $"{pair.Key}={pair.Value}"]);
    }

    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
