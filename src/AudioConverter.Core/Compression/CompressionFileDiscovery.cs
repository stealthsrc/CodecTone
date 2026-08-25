using AudioConverter.Core.Models;

namespace AudioConverter.Core.Compression;

public sealed record CompressionDiscoveredFile(
    string Path,
    string SourceRoot,
    string RelativePath);

public static class CompressionFileDiscovery
{
    public static IReadOnlyList<CompressionDiscoveredFile> Find(
        string source,
        bool recursive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        if (File.Exists(source))
        {
            _ = AudioFormats.FromPath(source);
            var root = Path.GetDirectoryName(Path.GetFullPath(source))!;
            return [new CompressionDiscoveredFile(Path.GetFullPath(source), root, Path.GetFileName(source))];
        }
        if (!Directory.Exists(source)) throw new FileNotFoundException("Compression source not found.", source);

        var rootDirectory = Path.GetFullPath(source);
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(rootDirectory, "*", searchOption)
            .Where(IsSupported)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new CompressionDiscoveredFile(
                path,
                rootDirectory,
                Path.GetRelativePath(rootDirectory, path)))
            .ToArray();
    }

    private static bool IsSupported(string path)
    {
        try { _ = AudioFormats.FromPath(path); return true; }
        catch (ArgumentException) { return false; }
    }
}
