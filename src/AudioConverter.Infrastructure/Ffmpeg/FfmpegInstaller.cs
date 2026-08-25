using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

namespace AudioConverter.Infrastructure.Ffmpeg;

public sealed class FfmpegInstallException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class FfmpegInstaller(HttpClient? client = null)
{
    public const string Version = "9.0.1";
    public const string ArchiveUrl = "https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-9.0.1-essentials_build.zip";
    public const string ArchiveSha256 = "fec81ae03971d9dd4be3ebe02e263bd2ec1d789483f931bdba5f5715e65da2e9";
    private const long MaxArchiveBytes = 160L * 1024 * 1024;
    private const long MaxExecutableBytes = 300L * 1024 * 1024;
    private readonly HttpClient httpClient = client ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });

    public async Task<FfmpegTools> InstallAsync(
        string? appDataRoot = null,
        IProgress<(double Fraction, string Status)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var root = appDataRoot ?? Storage.AppPaths.Root;
        var finalDirectory = Path.Combine(root, "runtime", "ffmpeg");
        var (existingFfmpeg, existingFfprobe) = FfmpegLocator.ManagedPaths(root);
        if (File.Exists(existingFfmpeg) && File.Exists(existingFfprobe))
        {
            await ValidateExecutableAsync(existingFfmpeg, "ffmpeg version", cancellationToken);
            await ValidateExecutableAsync(existingFfprobe, "ffprobe version", cancellationToken);
            return new FfmpegTools(existingFfmpeg, existingFfprobe);
        }
        if (Directory.Exists(finalDirectory))
        {
            throw new FfmpegInstallException("L’installation FFmpeg locale est incomplète. Supprimez le dossier runtime\\ffmpeg puis réessayez.");
        }

        var runtimeDirectory = Path.GetDirectoryName(finalDirectory)!;
        Directory.CreateDirectory(runtimeDirectory);
        var temporaryRoot = Path.Combine(runtimeDirectory, ".ffmpeg-install-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var archivePath = Path.Combine(temporaryRoot, "ffmpeg.zip");
            progress?.Report((0, $"Téléchargement de FFmpeg {Version}…"));
            await DownloadAsync(archivePath, progress, cancellationToken);
            progress?.Report((0.82, "Archive SHA-256 validée. Extraction…"));
            var stagedDirectory = Path.Combine(temporaryRoot, "ffmpeg");
            var tools = await ExtractAsync(archivePath, stagedDirectory, cancellationToken);
            progress?.Report((0.92, "Validation des exécutables…"));
            await ValidateExecutableAsync(tools.FfmpegPath, "ffmpeg version", cancellationToken);
            await ValidateExecutableAsync(tools.FfprobePath, "ffprobe version", cancellationToken);
            await File.WriteAllTextAsync(
                Path.Combine(stagedDirectory, "SOURCE.txt"),
                $"FFmpeg {Version} Essentials Build\nSource: {ArchiveUrl}\nSHA-256: {ArchiveSha256}\nLicense: GPLv3\n",
                cancellationToken);
            Directory.Move(stagedDirectory, finalDirectory);
            progress?.Report((1, "FFmpeg est installé et prêt."));
            var (ffmpeg, ffprobe) = FfmpegLocator.ManagedPaths(root);
            return new FfmpegTools(ffmpeg, ffprobe);
        }
        catch (FfmpegInstallException)
        {
            throw;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or HttpRequestException or InvalidDataException)
        {
            throw new FfmpegInstallException($"Installation de FFmpeg impossible : {error.Message}", error);
        }
        finally
        {
            if (Directory.Exists(temporaryRoot)) Directory.Delete(temporaryRoot, true);
        }
    }

    public static async Task VerifySha256Async(
        string path,
        string expected,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new FfmpegInstallException("Le SHA-256 de l’archive FFmpeg ne correspond pas. Rien n’a été installé.");
        }
    }

    private async Task DownloadAsync(
        string destination,
        IProgress<(double Fraction, string Status)>? progress,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(ArchiveUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var finalUri = response.RequestMessage?.RequestUri;
        if (finalUri is null || finalUri.Scheme != Uri.UriSchemeHttps || !finalUri.Host.Equals("www.gyan.dev", StringComparison.OrdinalIgnoreCase))
        {
            throw new FfmpegInstallException("Le téléchargement FFmpeg a été redirigé vers une adresse non approuvée.");
        }
        var total = response.Content.Headers.ContentLength;
        if (total > MaxArchiveBytes) throw new FfmpegInstallException("L’archive FFmpeg dépasse la limite de taille.");
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(destination);
        var buffer = new byte[1024 * 1024];
        long downloaded = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            downloaded += read;
            if (downloaded > MaxArchiveBytes) throw new FfmpegInstallException("L’archive FFmpeg dépasse la limite de taille.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            if (total > 0) progress?.Report((Math.Min(downloaded / (double)total.Value, 1) * 0.78, "Téléchargement de FFmpeg…"));
        }
        if (total is not null && downloaded != total) throw new FfmpegInstallException("Le téléchargement FFmpeg est incomplet.");
        await output.FlushAsync(cancellationToken);
        await VerifySha256Async(destination, ArchiveSha256, cancellationToken);
    }

    private static async Task<FfmpegTools> ExtractAsync(string archivePath, string destination, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var selected = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            var normalized = entry.FullName.Replace('\\', '/');
            if (!normalized.EndsWith("/bin/ffmpeg.exe", StringComparison.OrdinalIgnoreCase)
                && !normalized.EndsWith("/bin/ffprobe.exe", StringComparison.OrdinalIgnoreCase)) continue;
            var name = Path.GetFileName(normalized);
            if (!selected.TryAdd(name, entry)) throw new FfmpegInstallException($"L’archive contient plusieurs fichiers {name}.");
            if (entry.Length <= 2 || entry.Length > MaxExecutableBytes) throw new FfmpegInstallException($"La taille de {name} est invalide.");
        }
        if (!selected.ContainsKey("ffmpeg.exe") || !selected.ContainsKey("ffprobe.exe"))
        {
            throw new FfmpegInstallException("L’archive FFmpeg ne contient pas les deux exécutables attendus.");
        }
        var bin = Path.Combine(destination, "bin");
        Directory.CreateDirectory(bin);
        foreach (var pair in selected)
        {
            var outputPath = Path.Combine(bin, pair.Key);
            await using var input = pair.Value.Open();
            await using var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, true);
            await input.CopyToAsync(output, cancellationToken);
            await output.FlushAsync(cancellationToken);
            await using var check = File.OpenRead(outputPath);
            if (check.ReadByte() != 'M' || check.ReadByte() != 'Z') throw new FfmpegInstallException($"{pair.Key} n’est pas un exécutable Windows valide.");
        }
        return new FfmpegTools(Path.Combine(bin, "ffmpeg.exe"), Path.Combine(bin, "ffprobe.exe"));
    }

    private static async Task ValidateExecutableAsync(string path, string prefix, CancellationToken cancellationToken)
    {
        var info = FfmpegProcessRunner.CreateStartInfo(path, ["-version"], true);
        using var process = Process.Start(info) ?? throw new FfmpegInstallException($"{Path.GetFileName(path)} n’a pas pu démarrer.");
        var firstLine = await process.StandardOutput.ReadLineAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0 || firstLine is null || !firstLine.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new FfmpegInstallException($"{Path.GetFileName(path)} a échoué lors de sa validation.");
        }
    }
}
