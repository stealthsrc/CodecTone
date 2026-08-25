namespace AudioConverter.Core.Models;

public sealed record ConversionOptions(
    AudioFormat OutputFormat,
    string? Bitrate = null,
    int? SampleRate = null,
    int? BitDepth = null,
    bool PreserveMetadata = true,
    bool Overwrite = false);
