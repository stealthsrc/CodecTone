using System.Diagnostics;
using AudioConverter.Core.Models;
using System.Text.Json;
using AudioConverter.Infrastructure.Artwork;

namespace AudioConverter.Infrastructure.Ffmpeg;

public sealed class FfprobeService(string ffprobePath)
{
    public async Task<ProbeInfo> ProbeAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Fichier audio introuvable.", path);
        var arguments = new[]
        {
            "-v", "error", "-show_entries",
            "format=duration,size,bit_rate:format_tags:stream=index,codec_type,codec_name,sample_rate,channels,bits_per_sample,bits_per_raw_sample,bit_rate,width,height:stream_tags=comment,title:stream_disposition=attached_pic",
            "-of", "json", path,
        };
        var info = FfmpegProcessRunner.CreateStartInfo(ffprobePath, arguments, true);
        using var process = Process.Start(info) ?? throw new FfmpegExecutionException("ffprobe n’a pas pu démarrer.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await FfmpegProcessRunner.WaitForExitOrKillAsync(process, cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidDataException($"ffprobe a refusé ce fichier : {stderr.Trim()}");
        }
        return FfprobeParser.Parse(stdout);
    }

    public async Task<ArtworkImageProbe> ProbeImageAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Artwork image not found.", path);
        var arguments = new[] { "-v", "error", "-select_streams", "v:0", "-show_entries", "stream=codec_name,width,height", "-of", "json", path };
        var info = FfmpegProcessRunner.CreateStartInfo(ffprobePath, arguments, true);
        using var process = Process.Start(info) ?? throw new FfmpegExecutionException("ffprobe could not validate artwork.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await FfmpegProcessRunner.WaitForExitOrKillAsync(process, cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0) throw new InvalidDataException($"ffprobe rejected the artwork image: {stderr.Trim()}");
        using var document = JsonDocument.Parse(stdout);
        var streams = document.RootElement.GetProperty("streams");
        if (streams.GetArrayLength() == 0) throw new InvalidDataException("Extracted artwork has no image stream.");
        var stream = streams[0];
        var width = stream.TryGetProperty("width", out var widthValue) ? widthValue.GetInt32() : 0;
        var height = stream.TryGetProperty("height", out var heightValue) ? heightValue.GetInt32() : 0;
        if (width <= 0 || height <= 0) throw new InvalidDataException("Extracted artwork has invalid dimensions.");
        return new ArtworkImageProbe(
            stream.TryGetProperty("codec_name", out var codec) ? codec.GetString() ?? "unknown" : "unknown",
            width,
            height);
    }
}
