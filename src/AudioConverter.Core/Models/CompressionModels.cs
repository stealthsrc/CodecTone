namespace AudioConverter.Core.Models;

public enum CompressionProfile
{
    HighFidelity,
    Balanced,
    MaximumReduction,
    TargetTotalSize,
}

public sealed record CompressionOptions(
    AudioFormat OutputFormat,
    CompressionProfile Profile = CompressionProfile.HighFidelity,
    long? TargetTotalBytes = null,
    bool OptimizeArtwork = true,
    bool PreserveMetadata = true,
    bool Overwrite = false);

public sealed record CompressionSource(
    string Path,
    string RelativePath,
    AudioFormat Format,
    double DurationSeconds,
    long SizeBytes,
    int? AudioBitrateKbps,
    bool HasCoverArt);

public sealed record CompressionPlannedFile(
    CompressionSource Source,
    long EstimatedOutputBytes,
    bool ShouldSkip,
    string? SkipReason);

public sealed record CompressionPlan(
    CompressionOptions Options,
    IReadOnlyList<CompressionPlannedFile> Files,
    int? TargetAudioBitrateKbps,
    long OriginalTotalBytes,
    long EstimatedOutputBytes,
    long MinimumTargetBytes,
    double TotalDurationSeconds);
