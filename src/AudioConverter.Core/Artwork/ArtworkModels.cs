namespace AudioConverter.Core.Artwork;

public enum ArtworkOutputFormat { Original, Png, Jpeg, Webp }

public sealed record EmbeddedArtworkInfo(
    int StreamIndex,
    string CodecName,
    int? Width,
    int? Height,
    bool IsAttachedPicture,
    bool IsFrontCover);

public sealed record ArtworkSource(
    string Path,
    IReadOnlyDictionary<string, string> Tags,
    IReadOnlyList<EmbeddedArtworkInfo> Artwork);

public sealed record ArtworkPlannedAlbum(
    string SourcePath,
    string Artist,
    string Album,
    string OutputBaseName,
    EmbeddedArtworkInfo Artwork)
{
    public IReadOnlyList<ArtworkPlannedAlbum> Fallbacks { get; init; } = [];
}

public sealed record ArtworkSkippedSource(string SourcePath, string Reason);

public sealed record ArtworkPlan(
    IReadOnlyList<ArtworkPlannedAlbum> Albums,
    IReadOnlyList<ArtworkSkippedSource> Skipped);

public sealed record ArtworkExtractionOptions(
    ArtworkOutputFormat OutputFormat,
    int? MaximumDimension = null);

public sealed record ArtworkExtractionResult(
    ArtworkPlannedAlbum Album,
    string? OutputPath,
    string? Error,
    bool Skipped);
