using AudioConverter.Core.Artwork;

namespace AudioConverter.Core.Models;

public sealed record ProbeInfo(
    double DurationSeconds,
    string CodecName,
    int? SampleRate,
    int? BitDepth,
    bool HasCoverArt,
    IReadOnlyDictionary<string, string> Tags,
    long? SizeBytes = null,
    int? AudioBitrateKbps = null,
    int? ContainerBitrateKbps = null,
    int? Channels = null,
    IReadOnlyList<EmbeddedArtworkInfo>? EmbeddedArtwork = null)
{
    public IReadOnlyList<EmbeddedArtworkInfo> ArtworkStreams => EmbeddedArtwork ?? [];
}
