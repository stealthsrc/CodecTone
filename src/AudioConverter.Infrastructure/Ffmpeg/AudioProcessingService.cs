using System.Diagnostics;
using AudioConverter.Core.Models;
using AudioConverter.Infrastructure.Audio;

namespace AudioConverter.Infrastructure.Ffmpeg;

public sealed class AudioProcessingService(FfmpegTools tools)
{
    private readonly FfmpegProcessRunner runner = new();
    private readonly FfprobeService probe = new(tools.FfprobePath);

    public Task ConvertAsync(
        string input,
        string output,
        ConversionOptions options,
        double durationSeconds,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default) =>
        RunTransactionalAsync(
            input, output, options.Overwrite, durationSeconds, progress,
            (staged, token) => runner.RunAsync(tools.FfmpegPath, FfmpegCommandBuilder.BuildConversion(input, staged, options with { Overwrite = true }, true), durationSeconds, progress, token),
            cancellationToken);

    public Task CompressAsync(
        string input,
        string output,
        CompressionOptions options,
        int? targetBitrateKbps,
        double durationSeconds,
        bool hasCoverArt,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default) =>
        RunTransactionalAsync(
            input, output, options.Overwrite, durationSeconds, progress,
            (staged, token) => runner.RunAsync(
                tools.FfmpegPath,
                FfmpegCommandBuilder.BuildCompression(input, staged, options with { Overwrite = true }, targetBitrateKbps, true, hasCoverArt),
                durationSeconds,
                progress,
                token),
            cancellationToken);

    public Task TrimAsync(
        string input,
        string output,
        ConversionOptions options,
        TrimSelection trim,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default) =>
        RunTransactionalAsync(
            input, output, options.Overwrite, trim.DurationSeconds, progress,
            (staged, token) => runner.RunAsync(tools.FfmpegPath, FfmpegCommandBuilder.BuildTrim(input, staged, options with { Overwrite = true }, trim, true), trim.DurationSeconds, progress, token),
            cancellationToken);

    public Task RenderPreviewAsync(
        string input,
        string output,
        TrimSelection trim,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        return runner.RunAsync(tools.FfmpegPath, FfmpegCommandBuilder.BuildPreview(input, output, trim, true), trim.DurationSeconds, progress, cancellationToken);
    }

    public async Task<double[]> ExtractWaveformAsync(
        string input,
        int width,
        double durationSeconds,
        CancellationToken cancellationToken = default)
    {
        var source = await probe.ProbeAsync(input, cancellationToken);
        var expectedSamples = checked((long)Math.Ceiling(durationSeconds * (source.SampleRate ?? 44100) * (source.Channels ?? 1)));
        var info = FfmpegProcessRunner.CreateStartInfo(tools.FfmpegPath, FfmpegCommandBuilder.BuildWaveform(input, width, durationSeconds), true);
        using var process = Process.Start(info) ?? throw new FfmpegExecutionException("FFmpeg n’a pas pu extraire la waveform.");
        var peaksTask = WaveformAggregator.AggregateAsync(process.StandardOutput.BaseStream, width, expectedSamples, cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await Task.WhenAll(peaksTask, FfmpegProcessRunner.WaitForExitOrKillAsync(process, cancellationToken));
        var error = await errorTask;
        if (process.ExitCode != 0) throw new FfmpegExecutionException($"Extraction waveform impossible : {error.Trim()}");
        return await peaksTask;
    }

    private async Task RunTransactionalAsync(
        string input,
        string output,
        bool overwrite,
        double durationSeconds,
        IProgress<double>? progress,
        Func<string, CancellationToken, Task> produce,
        CancellationToken cancellationToken)
    {
        await TransactionalAudioOutput.RunAsync(
            input,
            output,
            overwrite,
            produce,
            async (staged, token) =>
            {
                var info = await probe.ProbeAsync(staged, token);
                var minimumDuration = durationSeconds <= 0
                    ? 0.001
                    : Math.Max(0.001, durationSeconds - Math.Max(0.05, durationSeconds * 0.02));
                if (info.DurationSeconds < minimumDuration)
                    throw new InvalidDataException("Encoded output did not contain valid audio.");
            },
            cancellationToken);
        progress?.Report(1);
    }
}
