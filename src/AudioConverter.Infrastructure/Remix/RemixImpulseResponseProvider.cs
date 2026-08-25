using System.Reflection;
using AudioConverter.Infrastructure.Storage;

namespace AudioConverter.Infrastructure.Remix;

public static class RemixImpulseResponseProvider
{
    private const string ResourceName = "AudioConverter.Infrastructure.Assets.remix-hall-ir.wav";

    public static string EnsureExtracted(string? root = null)
    {
        var directory = root ?? Path.Combine(AppPaths.Root, "remix", "ir");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "hall-v1.wav");
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Embedded remix impulse response is missing.");
        if (File.Exists(path) && new FileInfo(path).Length == resource.Length) return path;

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
