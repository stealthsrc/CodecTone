using System.Diagnostics;
using System.Runtime.InteropServices;
using AudioConverter.Core.Models;
using AudioConverter.Infrastructure.Audio;

namespace AudioConverter.Infrastructure.Ffmpeg;

public sealed class AudioProcessingService(FfmpegTools tools)
{
    private readonly FfmpegProcessRunner runner = new();

    public Task ConvertAsync(
        string input,
        string output,
        ConversionOptions options,
        double durationSeconds,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default) =>
        runner.RunAsync(tools.FfmpegPath, FfmpegCommandBuilder.BuildConversion(input, output, options, true), durationSeconds, progress, cancellationToken);

    public Task CompressAsync(
        string input,
        string output,
        CompressionOptions options,
        int? targetBitrateKbps,
        double durationSeconds,
        bool hasCoverArt,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default) =>
        runner.RunAsync(
            tools.FfmpegPath,
            FfmpegCommandBuilder.BuildCompression(input, output, options, targetBitrateKbps, true, hasCoverArt),
            durationSeconds,
            progress,
            cancellationToken);

    public Task TrimAsync(
        string input,
        string output,
        ConversionOptions options,
        TrimSelection trim,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default) =>
        runner.RunAsync(tools.FfmpegPath, FfmpegCommandBuilder.BuildTrim(input, output, options, trim, true), trim.DurationSeconds, progress, cancellationToken);

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
        CancellationToken cancellationToken = default)
    {
        var info = FfmpegProcessRunner.CreateStartInfo(tools.FfmpegPath, FfmpegCommandBuilder.BuildWaveform(input), true);
        using var process = Process.Start(info) ?? throw new FfmpegExecutionException("FFmpeg n’a pas pu extraire la waveform.");
        await using var memory = new MemoryStream();
        var copyTask = process.StandardOutput.BaseStream.CopyToAsync(memory, cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await Task.WhenAll(copyTask, process.WaitForExitAsync(cancellationToken));
        var error = await errorTask;
        if (process.ExitCode != 0) throw new FfmpegExecutionException($"Extraction waveform impossible : {error.Trim()}");
        var bytes = memory.ToArray();
        if (bytes.Length % 2 != 0) Array.Resize(ref bytes, bytes.Length - 1);
        return WaveformAggregator.Aggregate(MemoryMarshal.Cast<byte, short>(bytes), width);
    }
}
