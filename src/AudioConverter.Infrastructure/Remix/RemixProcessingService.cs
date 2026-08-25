using System.Diagnostics;
using System.Globalization;
using AudioConverter.Core.Models;
using AudioConverter.Core.Remix;
using AudioConverter.Infrastructure.Ffmpeg;
using AudioConverter.Infrastructure.Storage;

namespace AudioConverter.Infrastructure.Remix;

public sealed class RemixProcessingService(FfmpegTools tools)
{
    private const long MaximumCoverBytes = 20L * 1024 * 1024;
    private readonly FfmpegProcessRunner runner = new();
    private readonly FfprobeService probe = new(tools.FfprobePath);
    private readonly string impulseResponsePath = RemixImpulseResponseProvider.EnsureExtracted();

    public void CleanupTemporaryFiles()
    {
        try { File.Delete(AppPaths.RemixPreviewWave); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        if (!Directory.Exists(AppPaths.RemixStaging)) return;
        foreach (var file in Directory.EnumerateFiles(AppPaths.RemixStaging))
        {
            try { File.Delete(file); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public async Task<string> RenderPreviewAsync(
        string inputPath,
        IReadOnlyList<RemixEffect> effects,
        int sampleRate,
        double sourceDurationSeconds,
        double previewStartSeconds,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var output = AppPaths.RemixPreviewWave;
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        await runner.RunAsync(
            tools.FfmpegPath,
            RemixCommandBuilder.BuildPreview(
                inputPath,
                output,
                effects,
                sampleRate,
                sourceDurationSeconds,
                previewStartSeconds,
                20,
                impulseResponsePath),
            Math.Min(20, sourceDurationSeconds - previewStartSeconds),
            progress,
            cancellationToken);
        return output;
    }

    public async Task ExportAsync(
        string inputPath,
        string finalPath,
        IReadOnlyList<RemixEffect> effects,
        ConversionOptions encoding,
        RemixMetadata metadata,
        int sampleRate,
        double sourceDurationSeconds,
        bool sourceHasCover,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateCover(metadata);
        if (File.Exists(finalPath) && !encoding.Overwrite)
            throw new IOException($"Destination already exists: {finalPath}");

        Directory.CreateDirectory(AppPaths.RemixStaging);
        var extension = Path.GetExtension(finalPath);
        var stagedPath = Path.Combine(AppPaths.RemixStaging, Guid.NewGuid().ToString("N") + extension);
        var finalDirectory = Path.GetDirectoryName(Path.GetFullPath(finalPath))!;
        Directory.CreateDirectory(finalDirectory);
        var destinationTemporary = Path.Combine(
            finalDirectory,
            $".{Path.GetFileNameWithoutExtension(finalPath)}.{Guid.NewGuid():N}.tmp{extension}");
        try
        {
            var measurement = effects.OfType<LoudnessNormalizeEffect>().FirstOrDefault(effect => effect.Enabled) is { } normalize
                ? await AnalyzeLoudnessAsync(inputPath, effects, sampleRate, sourceDurationSeconds, normalize, cancellationToken)
                : null;
            var stageEncoding = encoding with { Overwrite = true };
            await runner.RunAsync(
                tools.FfmpegPath,
                RemixCommandBuilder.BuildExport(
                    inputPath,
                    stagedPath,
                    effects,
                    stageEncoding,
                    metadata,
                    sampleRate,
                    sourceDurationSeconds,
                    sourceHasCover,
                    measurement,
                    impulseResponsePath),
                RemixRackValidator.CalculateOutputDuration(sourceDurationSeconds, effects),
                progress,
                cancellationToken);
            var stagedProbe = await probe.ProbeAsync(stagedPath, cancellationToken);
            if (stagedProbe.DurationSeconds <= 0)
                throw new InvalidDataException("The staged remix has no valid audio duration.");

            await CopyAsync(stagedPath, destinationTemporary, cancellationToken);
            File.Move(destinationTemporary, finalPath, encoding.Overwrite);
        }
        finally
        {
            File.Delete(stagedPath);
            File.Delete(destinationTemporary);
        }
    }

    private async Task<LoudnessMeasurements> AnalyzeLoudnessAsync(
        string inputPath,
        IReadOnlyList<RemixEffect> effects,
        int sampleRate,
        double sourceDurationSeconds,
        LoudnessNormalizeEffect normalize,
        CancellationToken cancellationToken)
    {
        var normalizeIndex = effects.IndexOf(normalize);
        var preceding = effects.Take(normalizeIndex).ToArray();
        var loudnorm = $"loudnorm=I={normalize.TargetLufs.ToString("0.###", CultureInfo.InvariantCulture)}:TP=-1.5:LRA=11:print_format=json";
        var reverbCount = preceding.Count(effect => effect.Enabled && effect is ReverbEffect);
        var arguments = new List<string> { "-hide_banner", "-nostats", "-i", inputPath };
        if (reverbCount > 0)
        {
            for (var index = 0; index < reverbCount; index++) arguments.AddRange(["-i", impulseResponsePath]);
            var graph = RemixFilterBuilder.BuildGraph(
                preceding,
                sampleRate,
                sourceDurationSeconds,
                preview: true,
                Enumerable.Range(1, reverbCount).ToArray());
            arguments.AddRange([
                "-filter_complex", graph.Graph + $";[{graph.OutputLabel}]{loudnorm}[analysis]",
                "-map", "[analysis]", "-f", "null", "NUL",
            ]);
        }
        else
        {
            var filter = RemixFilterBuilder.Build(preceding, sampleRate, sourceDurationSeconds, preview: true);
            if (!string.IsNullOrEmpty(filter)) filter += ",";
            arguments.AddRange(["-map", "0:a:0", "-af", filter + loudnorm, "-f", "null", "NUL"]);
        }
        var info = FfmpegProcessRunner.CreateStartInfo(tools.FfmpegPath, arguments, true);
        using var process = Process.Start(info) ?? throw new FfmpegExecutionException("FFmpeg loudness analysis could not start.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await FfmpegProcessRunner.WaitForExitOrKillAsync(process, cancellationToken);
        _ = await outputTask;
        var error = await errorTask;
        if (process.ExitCode != 0) throw new FfmpegExecutionException($"Loudness analysis failed: {error.Trim()}");
        return LoudnessParser.Parse(error);
    }

    private static void ValidateCover(RemixMetadata metadata)
    {
        if (metadata.CoverAction != CoverArtAction.Replace) return;
        var path = metadata.CoverPath!;
        if (!File.Exists(path)) throw new FileNotFoundException("Replacement cover not found.", path);
        if (new FileInfo(path).Length > MaximumCoverBytes) throw new ArgumentException("Replacement cover exceeds 20 MiB.");
        if (Path.GetExtension(path).ToLowerInvariant() is not (".png" or ".jpg" or ".jpeg" or ".webp"))
            throw new ArgumentException("Replacement cover must be PNG, JPEG, or WebP.");
    }

    private static async Task CopyAsync(string source, string destination, CancellationToken cancellationToken)
    {
        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, true);
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, true);
        await input.CopyToAsync(output, cancellationToken);
        await output.FlushAsync(cancellationToken);
    }
}

internal static class RemixListExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> values, T value)
    {
        for (var index = 0; index < values.Count; index++)
            if (EqualityComparer<T>.Default.Equals(values[index], value)) return index;
        return -1;
    }
}
