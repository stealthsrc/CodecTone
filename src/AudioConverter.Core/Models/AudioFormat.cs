namespace AudioConverter.Core.Models;

public enum AudioFormat
{
    Mp3,
    Flac,
    Wav,
    Ogg,
    Aac,
    M4a,
}

public static class AudioFormats
{
    public static readonly IReadOnlyList<AudioFormat> All =
    [
        AudioFormat.Mp3,
        AudioFormat.Flac,
        AudioFormat.Wav,
        AudioFormat.Ogg,
        AudioFormat.Aac,
        AudioFormat.M4a,
    ];

    public static AudioFormat FromPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return extension switch
        {
            "mp3" => AudioFormat.Mp3,
            "flac" => AudioFormat.Flac,
            "wav" => AudioFormat.Wav,
            "ogg" => AudioFormat.Ogg,
            "aac" => AudioFormat.Aac,
            "m4a" => AudioFormat.M4a,
            _ => throw new ArgumentException(
                $"Unsupported format for '{Path.GetFileName(path)}'.",
                nameof(path)),
        };
    }

    public static string ToExtension(this AudioFormat format) => format switch
    {
        AudioFormat.Mp3 => "mp3",
        AudioFormat.Flac => "flac",
        AudioFormat.Wav => "wav",
        AudioFormat.Ogg => "ogg",
        AudioFormat.Aac => "aac",
        AudioFormat.M4a => "m4a",
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    public static bool IsLossy(this AudioFormat format) =>
        format is AudioFormat.Mp3 or AudioFormat.Aac or AudioFormat.Ogg or AudioFormat.M4a;
}
