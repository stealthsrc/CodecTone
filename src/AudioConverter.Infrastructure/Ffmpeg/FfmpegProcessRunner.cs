using System.Diagnostics;

namespace AudioConverter.Infrastructure.Ffmpeg;

public sealed class FfmpegExecutionException(string message) : Exception(message);

public sealed class FfmpegProcessRunner
{
    public async Task RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        double durationSeconds,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = CreateStartInfo(executable, arguments, redirectOutput: true);
        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start()) throw new FfmpegExecutionException("FFmpeg n’a pas pu démarrer.");
        }
        catch (Exception error) when (error is not FfmpegExecutionException)
        {
            throw new FfmpegExecutionException($"Impossible de lancer FFmpeg : {error.Message}");
        }

        progress?.Report(0);
        var diagnosticsTask = process.StandardError.ReadToEndAsync(cancellationToken);
        while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
        {
            var value = FfmpegProgressParser.Parse(line.Trim(), durationSeconds);
            if (value is not null) progress?.Report(value.Value);
        }
        await process.WaitForExitAsync(cancellationToken);
        var diagnostics = await diagnosticsTask;
        if (process.ExitCode != 0)
        {
            var tail = string.Join(Environment.NewLine, diagnostics.Split('\n').TakeLast(8)).Trim();
            throw new FfmpegExecutionException($"FFmpeg a échoué (code {process.ExitCode}) : {tail}");
        }
        progress?.Report(1);
    }

    public static ProcessStartInfo CreateStartInfo(
        string executable,
        IReadOnlyList<string> arguments,
        bool redirectOutput)
    {
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        return info;
    }
}
