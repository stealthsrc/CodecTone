using System.Text.Json;
using AudioConverter.Core.Models;

namespace AudioConverter.Infrastructure.Storage;

public sealed class JsonSettingsStore(string? root = null)
{
    private readonly string path = Path.Combine(root ?? AppPaths.Root, "settings.json");

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<AppSettings>(stream, cancellationToken: cancellationToken)
                ?? new AppSettings();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        var temporary = path + ".tmp";
        await using (var stream = File.Create(temporary))
        {
            await JsonSerializer.SerializeAsync(stream, settings, cancellationToken: cancellationToken);
        }
        File.Move(temporary, path, true);
    }
}
