using System.Globalization;
using AudioConverter.Core.Compression;
using AudioConverter.Core.Models;
using AudioConverter.Core.Validation;

namespace AudioConverter.Infrastructure.Ffmpeg;

public static class FfmpegCommandBuilder
{
    public static string[] BuildCompression(
        string inputPath,
        string outputPath,
        CompressionOptions options,
        int? targetBitrateKbps,
        bool showProgress,
        bool hasCoverArt = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(options);
        if (options.OutputFormat == AudioFormat.Wav)
            throw new ArgumentException("WAV is not a compression destination.");

        var args = new List<string>
        {
            "-hide_banner", "-loglevel", "error", options.Overwrite ? "-y" : "-n",
            "-i", inputPath, "-map", "0:a:0",
        };
        var artwork = hasCoverArt && SupportsArtwork(options.OutputFormat);
        if (options.PreserveMetadata)
        {
            args.AddRange(["-map_metadata", "0"]);
            if (artwork) args.AddRange(["-map", "0:v:disp:attached_pic?"]);
            else args.Add("-vn");
        }
        else
        {
            args.AddRange(["-vn", "-map_metadata", "-1"]);
        }

        AddCompressionEncoding(args, options, targetBitrateKbps);
        if (options.PreserveMetadata && artwork)
        {
            if (options.OptimizeArtwork)
            {
                args.AddRange([
                    "-vf", "scale=1200:1200:force_original_aspect_ratio=decrease",
                    "-c:v", "mjpeg", "-q:v", "3",
                ]);
            }
            else
            {
                args.AddRange(["-c:v", options.OutputFormat == AudioFormat.Flac ? "copy" : "mjpeg"]);
            }
            args.AddRange(["-disposition:v:0", "attached_pic"]);
        }

        AddProgress(args, showProgress);
        args.Add(outputPath);
        return [.. args];
    }

    public static string[] BuildConversion(
        string inputPath,
        string outputPath,
        ConversionOptions options,
        bool showProgress)
    {
        OptionValidator.Validate(options);
        var args = new List<string>
        {
            "-hide_banner", "-loglevel", "error", options.Overwrite ? "-y" : "-n",
            "-i", inputPath, "-map", "0:a:0",
        };

        if (options.PreserveMetadata)
        {
            args.AddRange(["-map_metadata", "0"]);
            if (SupportsArtwork(options.OutputFormat))
            {
                args.AddRange(["-map", "0:v:disp:attached_pic?"]);
            }
            else
            {
                args.Add("-vn");
            }
        }
        else
        {
            args.AddRange(["-vn", "-map_metadata", "-1"]);
        }

        AddEncoding(args, options);
        if (options.PreserveMetadata && SupportsArtwork(options.OutputFormat))
        {
            args.AddRange([
                "-c:v", options.OutputFormat == AudioFormat.Flac ? "copy" : "mjpeg",
                "-disposition:v:0", "attached_pic",
            ]);
        }

        AddProgress(args, showProgress);
        args.Add(outputPath);
        return [.. args];
    }

    public static string[] BuildTrim(
        string inputPath,
        string outputPath,
        ConversionOptions options,
        TrimSelection trim,
        bool showProgress)
    {
        var args = BuildConversion(inputPath, outputPath, options, false).ToList();
        args.RemoveAt(args.Count - 1);
        args.AddRange(["-af", BuildTrimFilter(trim)]);
        AddProgress(args, showProgress);
        args.Add(outputPath);
        return [.. args];
    }

    public static string[] BuildPreview(
        string inputPath,
        string outputPath,
        TrimSelection trim,
        bool showProgress)
    {
        var args = new List<string>
        {
            "-hide_banner", "-loglevel", "error", "-y", "-i", inputPath,
            "-map", "0:a:0", "-vn", "-af", BuildTrimFilter(trim),
            "-ac", "2", "-ar", "44100", "-c:a", "pcm_s16le",
        };
        AddProgress(args, showProgress);
        args.Add(outputPath);
        return [.. args];
    }

    public static string[] BuildWaveform(string inputPath) =>
    [
        "-hide_banner", "-loglevel", "error", "-i", inputPath, "-map", "0:a:0",
        "-vn", "-ac", "1", "-ar", "1000", "-f", "s16le", "pipe:1",
    ];

    private static void AddEncoding(List<string> args, ConversionOptions options)
    {
        switch (options.OutputFormat)
        {
            case AudioFormat.Mp3:
                args.AddRange(["-c:a", "libmp3lame", "-b:a", options.Bitrate ?? "192k"]);
                break;
            case AudioFormat.Aac:
                args.AddRange(["-c:a", "aac", "-b:a", options.Bitrate ?? "192k", "-f", "adts"]);
                break;
            case AudioFormat.Ogg:
                args.AddRange(["-c:a", "libvorbis", "-b:a", options.Bitrate ?? "192k"]);
                break;
            case AudioFormat.M4a:
                args.AddRange(["-c:a", "aac", "-b:a", options.Bitrate ?? "192k"]);
                break;
            case AudioFormat.Wav:
                var codec = options.BitDepth switch
                {
                    24 => "pcm_s24le",
                    32 => "pcm_s32le",
                    _ => "pcm_s16le",
                };
                args.AddRange(["-c:a", codec]);
                AddSampleRate(args, options.SampleRate);
                break;
            case AudioFormat.Flac:
                args.AddRange(["-c:a", "flac"]);
                AddSampleRate(args, options.SampleRate);
                if (options.BitDepth == 16)
                {
                    args.AddRange(["-sample_fmt", "s16"]);
                }
                else if (options.BitDepth == 24)
                {
                    args.AddRange(["-sample_fmt", "s32", "-bits_per_raw_sample", "24"]);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options));
        }
    }

    private static void AddCompressionEncoding(
        List<string> args,
        CompressionOptions options,
        int? targetBitrateKbps)
    {
        if (options.OutputFormat == AudioFormat.Flac)
        {
            args.AddRange([
                "-c:a", "flac",
                "-compression_level", CompressionProfiles.FlacLevel(options.Profile).ToString(CultureInfo.InvariantCulture),
            ]);
            return;
        }

        var codec = options.OutputFormat switch
        {
            AudioFormat.Mp3 => "libmp3lame",
            AudioFormat.Ogg => "libvorbis",
            AudioFormat.Aac or AudioFormat.M4a => "aac",
            _ => throw new ArgumentException("Unsupported compression destination."),
        };
        args.AddRange(["-c:a", codec]);

        if (options.Profile == CompressionProfile.TargetTotalSize)
        {
            if (targetBitrateKbps is null or <= 0)
                throw new ArgumentException("Target-size compression requires a calculated bitrate.");
            args.AddRange(["-b:a", $"{targetBitrateKbps.Value}k"]);
        }
        else if (options.OutputFormat is AudioFormat.Mp3 or AudioFormat.Ogg)
        {
            args.AddRange([
                "-q:a",
                CompressionProfiles.VbrQuality(options.OutputFormat, options.Profile).ToString(CultureInfo.InvariantCulture),
            ]);
        }
        else
        {
            args.AddRange([
                "-b:a",
                $"{CompressionProfiles.NominalBitrateKbps(options.OutputFormat, options.Profile)}k",
            ]);
        }

        if (options.OutputFormat == AudioFormat.Aac) args.AddRange(["-f", "adts"]);
    }

    private static void AddSampleRate(List<string> args, int? sampleRate)
    {
        if (sampleRate is not null)
        {
            args.AddRange(["-ar", sampleRate.Value.ToString(CultureInfo.InvariantCulture)]);
        }
    }

    private static string BuildTrimFilter(TrimSelection trim)
    {
        static string Number(double value) => value.ToString("0.000", CultureInfo.InvariantCulture);
        var filters = new List<string>
        {
            $"atrim=start={Number(trim.StartSeconds)}:end={Number(trim.EndSeconds)}",
            "asetpts=PTS-STARTPTS",
        };
        if (trim.FadeInSeconds > 0)
        {
            filters.Add($"afade=t=in:st=0:d={Number(trim.FadeInSeconds)}");
        }
        if (trim.FadeOutSeconds > 0)
        {
            filters.Add($"afade=t=out:st={Number(trim.DurationSeconds - trim.FadeOutSeconds)}:d={Number(trim.FadeOutSeconds)}");
        }
        return string.Join(',', filters);
    }

    private static bool SupportsArtwork(AudioFormat format) =>
        format is AudioFormat.Mp3 or AudioFormat.M4a or AudioFormat.Flac;

    private static void AddProgress(List<string> args, bool showProgress)
    {
        if (showProgress)
        {
            args.AddRange(["-progress", "pipe:1", "-nostats"]);
        }
    }
}
