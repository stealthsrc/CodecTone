using System.Text.RegularExpressions;

namespace AudioConverter.Core.Remix;

public static partial class RemixMetadataValidator
{
    public static void Validate(RemixMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        foreach (var key in metadata.CustomTags?.Keys ?? [])
        {
            if (string.IsNullOrWhiteSpace(key) || !TagKeyPattern().IsMatch(key))
                throw new ArgumentException($"Invalid custom metadata key: {key}");
        }
        if (metadata.CoverAction == CoverArtAction.Replace && string.IsNullOrWhiteSpace(metadata.CoverPath))
            throw new ArgumentException("A replacement cover path is required.");
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex TagKeyPattern();
}
