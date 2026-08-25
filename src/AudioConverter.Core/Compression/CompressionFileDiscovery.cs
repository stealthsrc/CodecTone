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
        return EnumerateFilesSafely(rootDirectory, recursive)
            .Where(IsSupported)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new CompressionDiscoveredFile(
                path,
                rootDirectory,
                Path.GetRelativePath(rootDirectory, path)))
            .ToArray();
    }

    private static IEnumerable<string> EnumerateFilesSafely(string root, bool recursive)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            string[] files;
            try { files = Directory.GetFiles(directory); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { continue; }
            foreach (var file in files) yield return file;
            if (!recursive) continue;

            string[] children;
            try { children = Directory.GetDirectories(directory); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { continue; }
            foreach (var child in children)
            {
                try
                {
                    if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0) pending.Push(child);
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
            }
        }
    }

    private static bool IsSupported(string path)
    {
        try { _ = AudioFormats.FromPath(path); return true; }
        catch (ArgumentException) { return false; }
    }
}
