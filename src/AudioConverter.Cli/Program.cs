using AudioConverter.Core.Models;
using AudioConverter.Core.Paths;
using AudioConverter.Core.Compression;
using AudioConverter.Infrastructure.Ffmpeg;

return await AudioConverterCli.RunAsync(args);

internal static class AudioConverterCli
{
    private static readonly HashSet<string> SupportedOptions =
    [
        "--format", "-f", "--output-dir", "-o", "--suffix", "--bitrate",
        "--sample-rate", "--bit-depth", "--no-metadata", "--overwrite",
        "--start", "--end", "--fade-in", "--fade-out", "--install-ffmpeg", "--help", "-h",
        "--compress-profile", "--target-total-mb", "--recursive", "--optimize-artwork", "--no-optimize-artwork",
    ];

    public static async Task<int> RunAsync(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += handler;
        try { return await RunCoreAsync(args, cancellation.Token); }
        finally { Console.CancelKeyPress -= handler; }
    }

    private static async Task<int> RunCoreAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintHelp(); return 0;
        }
        try
        {
            if (args.Contains("--install-ffmpeg"))
            {
                var progress = new Progress<(double Fraction, string Status)>(item => Console.WriteLine($"{item.Fraction,6:P0}  {item.Status}"));
                var installed = await new FfmpegInstaller().InstallAsync(progress: progress, cancellationToken: cancellationToken);
                Console.WriteLine($"Installed: {installed.FfmpegPath}"); return 0;
            }

            if (args.Contains("--compress-profile") || args.Contains("--target-total-mb"))
            {
                return await RunCompressionAsync(args, cancellationToken);
            }

            var source = args[0];
            if (source.StartsWith('-')) throw new ArgumentException("The first argument must be an audio file or folder.");
            var formatText = Value(args, "--format", "-f") ?? throw new ArgumentException("--format is required.");
            if (!Enum.TryParse<AudioFormat>(formatText, true, out var format) || !AudioFormats.All.Contains(format)) throw new ArgumentException($"Unsupported destination format: {formatText}");
            ValidateKnownOptions(args);

            var outputDirectory = Value(args, "--output-dir", "-o");
            var suffix = Value(args, "--suffix") ?? "";
            var bitrate = format.IsLossy() ? Value(args, "--bitrate") ?? "192k" : null;
            var sampleRate = ParseInt(Value(args, "--sample-rate"), "sample rate");
            var bitDepth = ParseInt(Value(args, "--bit-depth"), "bit depth");
            var options = new ConversionOptions(format, bitrate, format.IsLossy() ? null : sampleRate, format.IsLossy() ? null : bitDepth, !args.Contains("--no-metadata"), args.Contains("--overwrite"));
            var trimStart = ParseDouble(Value(args, "--start"), "start");
            var trimEnd = ParseDouble(Value(args, "--end"), "end");
            var fadeIn = ParseDouble(Value(args, "--fade-in"), "fade in") ?? 0;
            var fadeOut = ParseDouble(Value(args, "--fade-out"), "fade out") ?? 0;

            var tools = FfmpegLocator.Find(); var probe = new FfprobeService(tools.FfprobePath); var audio = new AudioProcessingService(tools);
            var files = Sources(source).ToArray();
            if (files.Length == 0) throw new InvalidDataException("No supported audio file was found.");
            var succeeded = 0; var failed = 0;
            for (var index = 0; index < files.Length; index++)
            {
                var input = files[index];
                try
                {
                    var info = await probe.ProbeAsync(input, cancellationToken);
                    var directory = outputDirectory ?? Path.Combine(Path.GetDirectoryName(input)!, "converted");
                    Directory.CreateDirectory(directory);
                    var output = OutputPathBuilder.Build(input, directory, format, suffix);
                    var lastPercent = -1;
                    var progress = new Progress<double>(fraction =>
                    {
                        var percent = (int)(fraction * 100);
                        if (percent >= lastPercent + 10 || percent == 100) { lastPercent = percent; Console.Write($"\r[{index + 1}/{files.Length}] {Path.GetFileName(input)} {percent,3}%"); }
                    });
                    if (trimStart is not null || trimEnd is not null || fadeIn > 0 || fadeOut > 0)
                    {
                        var selection = TrimSelection.Create(trimStart ?? 0, trimEnd ?? info.DurationSeconds, fadeIn, fadeOut);
                        await audio.TrimAsync(input, output, options, selection, progress, cancellationToken);
                    }
                    else
                    {
                        await audio.ConvertAsync(input, output, options, info.DurationSeconds, progress, cancellationToken);
                    }
                    Console.WriteLine($"\rOK   {input} -> {output}                              "); succeeded++;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception error) { Console.WriteLine($"FAIL {input}: {error.Message}"); failed++; }
            }
            Console.WriteLine($"Completed: {succeeded} succeeded, {failed} failed"); return failed == 0 ? 0 : 2;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Cancelled."); return 130; }
        catch (Exception error) { Console.Error.WriteLine($"Error: {error.Message}"); return 1; }
    }

    private static IEnumerable<string> Sources(string source)
    {
        if (File.Exists(source)) return [source];
        if (!Directory.Exists(source)) throw new FileNotFoundException("Source not found.", source);
        return Directory.EnumerateFiles(source).Where(path => { try { _ = AudioFormats.FromPath(path); return true; } catch { return false; } });
    }

    private static async Task<int> RunCompressionAsync(string[] args, CancellationToken cancellationToken)
    {
        var source = args[0];
        if (source.StartsWith('-')) throw new ArgumentException("The first argument must be an audio file or folder.");
        ValidateKnownOptions(args);
        var formatText = Value(args, "--format", "-f") ?? throw new ArgumentException("--format is required.");
        if (!Enum.TryParse<AudioFormat>(formatText, true, out var format)
            || !AudioFormats.All.Contains(format)
            || format == AudioFormat.Wav)
            throw new ArgumentException($"Unsupported compression destination: {formatText}");

        var profileText = Value(args, "--compress-profile");
        var targetText = Value(args, "--target-total-mb");
        if (profileText is not null && targetText is not null)
            throw new ArgumentException("--compress-profile and --target-total-mb are mutually exclusive.");
        var profile = targetText is not null
            ? CompressionProfile.TargetTotalSize
            : ParseCompressionProfile(profileText ?? "high");
        var targetBytes = targetText is null
            ? null
            : (long?)(ParseDouble(targetText, "target total size")!.Value * 1024 * 1024);
        var optimizeArtwork = !args.Contains("--no-optimize-artwork");
        var options = new CompressionOptions(
            format,
            profile,
            targetBytes,
            optimizeArtwork,
            !args.Contains("--no-metadata"),
            args.Contains("--overwrite"));

        var outputRoot = Value(args, "--output-dir", "-o") ?? DefaultCompressionOutput(source);
        var suffix = Value(args, "--suffix") ?? "";
        var discovered = CompressionFileDiscovery.Find(source, recursive: true)
            .Where(file => !IsInside(file.Path, outputRoot))
            .ToArray();
        if (discovered.Length == 0) throw new InvalidDataException("No supported audio file was found.");

        var tools = FfmpegLocator.Find();
        var probe = new FfprobeService(tools.FfprobePath);
        var audio = new AudioProcessingService(tools);
        var inputs = new List<CompressionSource>();
        var failures = new List<string>();
        foreach (var file in discovered)
        {
            try
            {
                var info = await probe.ProbeAsync(file.Path, cancellationToken);
                inputs.Add(new CompressionSource(
                    file.Path,
                    file.RelativePath,
                    AudioFormats.FromPath(file.Path),
                    info.DurationSeconds,
                    info.SizeBytes ?? new FileInfo(file.Path).Length,
                    info.AudioBitrateKbps,
                    info.HasCoverArt));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception error)
            {
                failures.Add($"FAIL {file.Path}: {error.Message}");
            }
        }
        if (inputs.Count == 0) throw new InvalidDataException("No valid audio file remained after ffprobe validation.");

        var plan = CompressionPlanner.Create(inputs, options);
        Console.WriteLine($"Original: {FormatBytes(plan.OriginalTotalBytes)}");
        Console.WriteLine($"Estimated output: {FormatBytes(plan.EstimatedOutputBytes)}");
        if (inputs.Any(item => item.Format.IsLossy()) && format.IsLossy())
            Console.WriteLine("Warning: lossy-to-lossy compression can reduce audio quality.");

        var active = plan.Files.Where(file => !file.ShouldSkip).ToArray();
        var totalDuration = active.Sum(file => file.Source.DurationSeconds);
        var completedDuration = 0d;
        var succeeded = 0;
        var skipped = plan.Files.Count(file => file.ShouldSkip);
        long originalProcessedBytes = 0;
        long outputBytes = 0;
        foreach (var file in plan.Files)
        {
            if (file.ShouldSkip)
            {
                Console.WriteLine($"SKIP {file.Source.Path}: {file.SkipReason}");
                continue;
            }
            try
            {
                var output = OutputPathBuilder.BuildCompressed(
                    file.Source.Path,
                    discovered.First(item => item.Path.Equals(file.Source.Path, StringComparison.OrdinalIgnoreCase)).SourceRoot,
                    outputRoot,
                    format,
                    suffix);
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                var baseDuration = completedDuration;
                var progress = new Progress<double>(fraction =>
                {
                    var overall = totalDuration <= 0 ? 1 : (baseDuration + fraction * file.Source.DurationSeconds) / totalDuration;
                    Console.Write($"\r{overall,6:P0}  {Path.GetFileName(file.Source.Path)}");
                });
                await audio.CompressAsync(
                    file.Source.Path,
                    output,
                    options,
                    plan.TargetAudioBitrateKbps,
                    file.Source.DurationSeconds,
                    file.Source.HasCoverArt,
                    progress,
                    cancellationToken);
                completedDuration += file.Source.DurationSeconds;
                originalProcessedBytes += file.Source.SizeBytes;
                outputBytes += new FileInfo(output).Length;
                succeeded++;
                Console.WriteLine($"\rOK   {file.Source.Path} -> {output}                              ");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception error)
            {
                completedDuration += file.Source.DurationSeconds;
                failures.Add($"FAIL {file.Source.Path}: {error.Message}");
            }
        }

        foreach (var failure in failures) Console.WriteLine(failure);
        var savings = CompressionSavings.Calculate(originalProcessedBytes, outputBytes);
        Console.WriteLine($"Completed: {succeeded} succeeded, {failures.Count} failed, {skipped} skipped");
        Console.WriteLine($"Final: {FormatBytes(outputBytes)} · saved {FormatBytes(savings.SavedBytes)} ({savings.ReductionPercent:0.0}%)");
        return failures.Count == 0 ? 0 : 2;
    }

    private static CompressionProfile ParseCompressionProfile(string value) => value.ToLowerInvariant() switch
    {
        "high" or "high-fidelity" => CompressionProfile.HighFidelity,
        "balanced" => CompressionProfile.Balanced,
        "small" or "maximum-reduction" => CompressionProfile.MaximumReduction,
        _ => throw new ArgumentException($"Unknown compression profile: {value}"),
    };

    private static string DefaultCompressionOutput(string source) => File.Exists(source)
        ? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(source))!, "compressed")
        : Path.Combine(Path.GetFullPath(source), "compressed");

    private static bool IsInside(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatBytes(long bytes) => $"{bytes / 1024d / 1024d:0.00} MiB";
    private static string? Value(string[] args, params string[] names)
    {
        for (var index = 0; index < args.Length; index++) if (names.Contains(args[index])) { if (index + 1 >= args.Length || args[index + 1].StartsWith('-')) throw new ArgumentException($"{args[index]} requires a value."); return args[index + 1]; }
        return null;
    }
    private static int? ParseInt(string? value, string label) => value is null ? null : int.TryParse(value, out var parsed) ? parsed : throw new ArgumentException($"Invalid {label}: {value}");
    private static double? ParseDouble(string? value, string label) => value is null ? null : double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : throw new ArgumentException($"Invalid {label}: {value}");
    private static void ValidateKnownOptions(string[] args)
    {
        for (var index = 1; index < args.Length; index++) if (args[index].StartsWith('-') && !SupportedOptions.Contains(args[index])) throw new ArgumentException($"Unknown option: {args[index]}");
    }
    private static void PrintHelp() => Console.WriteLine("""
CodecTone.Cli <file-or-folder> --format <mp3|flac|wav|ogg|aac|m4a> [options]

Options:
  -o, --output-dir <folder>  Destination folder; default: converted beside source
  --suffix <text>            Text added before the extension
  --bitrate <rate>           MP3/AAC/OGG/M4A bitrate; default: 192k
  --sample-rate <hz>         WAV/FLAC sample rate
  --bit-depth <16|24|32>     WAV/FLAC bit depth; FLAC supports 16 or 24
  --start <seconds>          Start an accurate re-encoded cut
  --end <seconds>            End the cut; default: source duration
  --fade-in <seconds>        Apply a fade at the beginning of the cut
  --fade-out <seconds>       Apply a fade at the end of the cut
  --no-metadata              Remove source metadata and cover art
  --overwrite                Replace existing destination files
  --install-ffmpeg           Download and verify the pinned FFmpeg build

Compression:
  --compress-profile <high|balanced|small>  Compress recursively with a quality profile
  --target-total-mb <mb>                     Compress recursively toward a total folder budget
  --recursive                                Explicit recursive folder processing
  --optimize-artwork                         Optimize cover art (enabled by default)
  --no-optimize-artwork                      Preserve cover-art dimensions
""");
}
