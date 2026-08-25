using AudioConverter.Core.Models;

namespace AudioConverter.Core.Compression;

public static class CompressionProfiles
{
    public static int NominalBitrateKbps(AudioFormat format, CompressionProfile profile) =>
        (format, profile) switch
        {
            (AudioFormat.Mp3, CompressionProfile.HighFidelity) => 190,
            (AudioFormat.Mp3, CompressionProfile.Balanced) => 165,
            (AudioFormat.Mp3, CompressionProfile.MaximumReduction) => 115,
            (AudioFormat.Ogg, CompressionProfile.HighFidelity) => 256,
            (AudioFormat.Ogg, CompressionProfile.Balanced) => 192,
            (AudioFormat.Ogg, CompressionProfile.MaximumReduction) => 128,
            (AudioFormat.Aac or AudioFormat.M4a, CompressionProfile.HighFidelity) => 256,
            (AudioFormat.Aac or AudioFormat.M4a, CompressionProfile.Balanced) => 192,
            (AudioFormat.Aac or AudioFormat.M4a, CompressionProfile.MaximumReduction) => 128,
            _ => throw new ArgumentException("This profile does not define a lossy bitrate."),
        };

    public static int FlacLevel(CompressionProfile profile) => profile switch
    {
        CompressionProfile.HighFidelity => 8,
        CompressionProfile.Balanced => 10,
        CompressionProfile.MaximumReduction => 12,
        _ => throw new ArgumentException("Target-size compression is not available for FLAC."),
    };

    public static int MinimumBitrateKbps(AudioFormat format) => format switch
    {
        AudioFormat.Mp3 or AudioFormat.Ogg => 64,
        AudioFormat.Aac or AudioFormat.M4a => 48,
        _ => throw new ArgumentException("A target bitrate requires a lossy destination."),
    };

    public static int MaximumBitrateKbps(AudioFormat format) => format switch
    {
        AudioFormat.Mp3 => 320,
        AudioFormat.Ogg => 500,
        AudioFormat.Aac or AudioFormat.M4a => 512,
        _ => throw new ArgumentException("A target bitrate requires a lossy destination."),
    };

    public static int VbrQuality(AudioFormat format, CompressionProfile profile) =>
        (format, profile) switch
        {
            (AudioFormat.Mp3, CompressionProfile.HighFidelity) => 2,
            (AudioFormat.Mp3, CompressionProfile.Balanced) => 4,
            (AudioFormat.Mp3, CompressionProfile.MaximumReduction) => 6,
            (AudioFormat.Ogg, CompressionProfile.HighFidelity) => 8,
            (AudioFormat.Ogg, CompressionProfile.Balanced) => 6,
            (AudioFormat.Ogg, CompressionProfile.MaximumReduction) => 4,
            _ => throw new ArgumentException("VBR quality is defined only for MP3 and OGG profiles."),
        };
}
