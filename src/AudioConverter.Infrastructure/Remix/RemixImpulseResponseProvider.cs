using System.Reflection;
using System.Security.Cryptography;
using AudioConverter.Infrastructure.Storage;

namespace AudioConverter.Infrastructure.Remix;

public static class RemixImpulseResponseProvider
{
    private const string ResourceName = "AudioConverter.Infrastructure.Assets.remix-hall-ir.wav";

    private static readonly object Gate = new();

    public static string EnsureExtracted(string? root = null)
    {
        lock (Gate) return EnsureExtractedCore(root);
    }

    private static string EnsureExtractedCore(string? root)
    {
        var directory = root ?? Path.Combine(AppPaths.Root, "remix", "ir");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "hall-v1.wav");
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Embedded remix impulse response is missing.");
        var expectedHash = SHA256.HashData(resource);
        resource.Position = 0;
        if (File.Exists(path))
        {
            using var existing = File.OpenRead(path);
            if (CryptographicOperations.FixedTimeEquals(expectedHash, SHA256.HashData(existing))) return path;
        }

        var temporary = path + ".tmp";
        try
        {
            using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                resource.CopyTo(output);
            File.Move(temporary, path, true);
            return path;
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
