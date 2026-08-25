using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AudioConverter.Core.Remix;
using AudioConverter.Infrastructure.Storage;

namespace AudioConverter.Infrastructure.Remix;

public sealed class RemixAnalysisCache(string? directory = null)
{
    private const int SchemaVersion = 1;
    private readonly string directory = directory ?? AppPaths.RemixAnalysis;
    private readonly SemaphoreSlim gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<AudioAnalysis?> TryReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var source = new FileInfo(sourcePath);
            if (!source.Exists) return null;
            var cachePath = GetCachePath(source.FullName);
            if (!File.Exists(cachePath)) return null;
            await using var stream = File.OpenRead(cachePath);
            var entry = await JsonSerializer.DeserializeAsync<CacheEntry>(stream, JsonOptions, cancellationToken);
            return entry is not null
                && entry.Version == SchemaVersion
                && entry.SourceLength == source.Length
                && entry.SourceWriteUtcTicks == source.LastWriteTimeUtc.Ticks
                ? entry.Analysis
                : null;
        }
        catch (JsonException) { return null; }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
        finally { gate.Release(); }
    }

    public async Task WriteAsync(string sourcePath, AudioAnalysis analysis, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        string? temporary = null;
        try
        {
            var source = new FileInfo(sourcePath);
            if (!source.Exists) throw new FileNotFoundException("Remix source not found.", sourcePath);
            Directory.CreateDirectory(directory);
            var destination = GetCachePath(source.FullName);
            temporary = destination + $".{Guid.NewGuid():N}.tmp";
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true))
                await JsonSerializer.SerializeAsync(stream, new CacheEntry(SchemaVersion, source.Length, source.LastWriteTimeUtc.Ticks, analysis), JsonOptions, cancellationToken);
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (temporary is not null) File.Delete(temporary);
            gate.Release();
        }
    }

    private string GetCachePath(string sourcePath)
    {
        var normalized = Path.GetFullPath(sourcePath).ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return Path.Combine(directory, hash + ".json");
    }

    private sealed record CacheEntry(int Version, long SourceLength, long SourceWriteUtcTicks, AudioAnalysis Analysis);
}
