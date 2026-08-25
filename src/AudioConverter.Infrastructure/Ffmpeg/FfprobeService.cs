using System.Diagnostics;
using AudioConverter.Core.Models;

namespace AudioConverter.Infrastructure.Ffmpeg;

public sealed class FfprobeService(string ffprobePath)
{
    public async Task<ProbeInfo> ProbeAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Fichier audio introuvable.", path);
        var arguments = new[]
        {
            "-v", "error", "-show_entries",
            "format=duration,size,bit_rate:format_tags:stream=codec_type,codec_name,sample_rate,bits_per_sample,bits_per_raw_sample,bit_rate:stream_disposition=attached_pic",
            "-of", "json", path,
        };
        var info = FfmpegProcessRunner.CreateStartInfo(ffprobePath, arguments, true);
        using var process = Process.Start(info) ?? throw new FfmpegExecutionException("ffprobe n’a pas pu démarrer.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidDataException($"ffprobe a refusé ce fichier : {stderr.Trim()}");
        }
        return FfprobeParser.Parse(stdout);
    }
}
