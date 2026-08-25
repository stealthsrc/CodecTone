using System.Diagnostics;
using AudioConverter.Core.Remix;
using AudioConverter.Infrastructure.Ffmpeg;

namespace AudioConverter.Infrastructure.Remix;

public sealed class AdaptiveAudioAnalyzer
{
    private readonly FfmpegTools tools;
    private readonly FfprobeService probe;
    private readonly RemixAnalysisCache cache;

    public AdaptiveAudioAnalyzer(FfmpegTools tools, RemixAnalysisCache? cache = null)
    {
        this.tools = tools;
        probe = new FfprobeService(tools.FfprobePath);
        this.cache = cache ?? new RemixAnalysisCache();
    }

    public async Task<AudioAnalysis> AnalyzeAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (await cache.TryReadAsync(sourcePath, cancellationToken) is { } cached) return cached;

        var probeTask = probe.ProbeAsync(sourcePath, cancellationToken);
        var loudnessTask = CaptureAsync(LoudnessArguments(sourcePath), cancellationToken);
        var dynamicsTask = CaptureAsync(DynamicsArguments(sourcePath), cancellationToken);
        var spectralTask = CaptureAsync(SpectralArguments(sourcePath), cancellationToken);
        await Task.WhenAll(probeTask, loudnessTask, dynamicsTask, spectralTask);

        var info = await probeTask;
        var loudness = LoudnessParser.Parse(await loudnessTask);
        var dynamics = RemixAnalysisParser.ParseDynamics(await dynamicsTask);
        var spectral = RemixAnalysisParser.ParseSpectral(await spectralTask);
        var analysis = new AudioAnalysis(
            FiniteOr(loudness.InputIntegrated, -70),
            FiniteOr(loudness.InputTruePeak, -70),
            FiniteOr(loudness.InputLoudnessRange, 0),
            FiniteOr(dynamics.CrestFactorDb, 0),
            FiniteOr(spectral.LowEnergyDb, -120),
            FiniteOr(spectral.MidEnergyDb, -120),
            FiniteOr(spectral.HighEnergyDb, -120),
            FiniteOr(spectral.StereoWidth, 0),
            info.DurationSeconds,
            info.SampleRate ?? 44_100,
            info.Channels ?? 2);
        await cache.WriteAsync(sourcePath, analysis, cancellationToken);
        return analysis;
    }

    private static double FiniteOr(double value, double fallback) => double.IsFinite(value) ? value : fallback;

    private async Task<string> CaptureAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var info = FfmpegProcessRunner.CreateStartInfo(tools.FfmpegPath, arguments, true);
        using var process = new Process { StartInfo = info };
        if (!process.Start()) throw new FfmpegExecutionException("FFmpeg analysis could not start.");
        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
        });
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        _ = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0) throw new FfmpegExecutionException($"FFmpeg analysis failed: {stderr.Trim()}");
        return stderr;
    }

    private static string[] LoudnessArguments(string path) =>
    [
        "-hide_banner", "-nostats", "-i", path, "-map", "0:a:0",
        "-af", "loudnorm=I=-14:TP=-1.5:LRA=11:print_format=json", "-f", "null", "NUL",
    ];

    private static string[] DynamicsArguments(string path) =>
    [
        "-hide_banner", "-nostats", "-i", path, "-map", "0:a:0",
        "-af", "astats=metadata=0:reset=0:measure_overall=Peak_level+RMS_level", "-f", "null", "NUL",
    ];

    private static string[] SpectralArguments(string path) =>
    [
        "-hide_banner", "-nostats", "-i", path,
        "-filter_complex",
        "[0:a]aformat=channel_layouts=stereo,asplit=5[li][mi][hi][ci][si];"
        + "[li]lowpass=f=250,astats=metadata=0:reset=0:measure_overall=RMS_level[low];"
        + "[mi]highpass=f=250,lowpass=f=4000,astats=metadata=0:reset=0:measure_overall=RMS_level[mid];"
        + "[hi]highpass=f=4000,astats=metadata=0:reset=0:measure_overall=RMS_level[high];"
        + "[ci]pan=mono|c0=0.5*c0+0.5*c1,astats=metadata=0:reset=0:measure_overall=RMS_level[center];"
        + "[si]pan=mono|c0=0.5*c0-0.5*c1,astats=metadata=0:reset=0:measure_overall=RMS_level[side]",
        "-map", "[low]", "-map", "[mid]", "-map", "[high]", "-map", "[center]", "-map", "[side]",
        "-f", "null", "NUL",
    ];
}
