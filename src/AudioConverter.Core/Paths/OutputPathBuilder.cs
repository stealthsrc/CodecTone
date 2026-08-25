using AudioConverter.Core.Models;

namespace AudioConverter.Core.Paths;

public static class OutputPathBuilder
{
    private static readonly char[] InvalidSuffixCharacters = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    public static string Build(
        string source,
        string outputDirectory,
        AudioFormat outputFormat,
        string suffix = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(suffix);

        if (suffix.IndexOfAny(InvalidSuffixCharacters) >= 0)
        {
            throw new ArgumentException("Suffix contains an invalid file-name character.", nameof(suffix));
        }

        var fileName = Path.GetFileNameWithoutExtension(source) + suffix + "." + outputFormat.ToExtension();
        return Path.Combine(outputDirectory, fileName);
    }

    public static string BuildCompressed(
        string source,
        string sourceRoot,
        string outputRoot,
        AudioFormat outputFormat,
        string suffix = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);
        var relative = Path.GetRelativePath(sourceRoot, source);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Source must be inside the selected source root.", nameof(source));

        var relativeDirectory = Path.GetDirectoryName(relative);
        var destinationDirectory = string.IsNullOrEmpty(relativeDirectory)
            ? outputRoot
            : Path.Combine(outputRoot, relativeDirectory);
        return Build(source, destinationDirectory, outputFormat, suffix);
    }
}
