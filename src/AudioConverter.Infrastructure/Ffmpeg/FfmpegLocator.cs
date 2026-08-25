namespace AudioConverter.Infrastructure.Ffmpeg;

public sealed record FfmpegTools(string FfmpegPath, string FfprobePath);

public sealed class FfmpegDependencyException(string message) : Exception(message);

public static class FfmpegLocator
{
    public static FfmpegTools Find(
        string? appDataRoot = null,
        Func<string, string?>? pathResolver = null)
    {
        var bin = Path.Combine(
            appDataRoot ?? Storage.AppPaths.Root,
            "runtime", "ffmpeg", "bin");
        var managedFfmpeg = Path.Combine(bin, "ffmpeg.exe");
        var managedFfprobe = Path.Combine(bin, "ffprobe.exe");
        if (File.Exists(managedFfmpeg) && File.Exists(managedFfprobe))
        {
            return new FfmpegTools(managedFfmpeg, managedFfprobe);
        }

        pathResolver ??= ResolveFromPath;
        var systemFfmpeg = pathResolver("ffmpeg.exe") ?? pathResolver("ffmpeg");
        var systemFfprobe = pathResolver("ffprobe.exe") ?? pathResolver("ffprobe");
        if (systemFfmpeg is not null && systemFfprobe is not null
            && Path.GetDirectoryName(Path.GetFullPath(systemFfmpeg))?.Equals(
                Path.GetDirectoryName(Path.GetFullPath(systemFfprobe)), StringComparison.OrdinalIgnoreCase) == true)
        {
            return new FfmpegTools(systemFfmpeg, systemFfprobe);
        }

        throw new FfmpegDependencyException(
            "FFmpeg et ffprobe sont absents. Utilisez le bouton Installer FFmpeg ou ajoutez-les au PATH Windows.");
    }

    public static (string Ffmpeg, string Ffprobe) ManagedPaths(string? appDataRoot = null)
    {
        var bin = Path.Combine(appDataRoot ?? Storage.AppPaths.Root, "runtime", "ffmpeg", "bin");
        return (Path.Combine(bin, "ffmpeg.exe"), Path.Combine(bin, "ffprobe.exe"));
    }

    private static string? ResolveFromPath(string executable)
    {
        foreach (var part in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
            var candidate = Path.Combine(part.Trim(), executable);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }
}
