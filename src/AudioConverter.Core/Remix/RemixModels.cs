namespace AudioConverter.Core.Remix;

public enum RemixPreset
{
    Custom,
    BassBoost,
    SlowedReverb,
    SpedUpReverb,
}

public enum RemixEffectKind
{
    TempoPitch,
    Bass,
    Equalizer,
    Reverb,
    Echo,
    Volume,
    FadeIn,
    FadeOut,
    LoudnessNormalize,
}

public abstract record RemixEffect(RemixEffectKind Kind, bool Enabled = true);

public sealed record TempoPitchEffect(double Rate, bool IsEnabled = true)
    : RemixEffect(RemixEffectKind.TempoPitch, IsEnabled);

public sealed record BassEffect(double GainDb, double FrequencyHz, bool IsEnabled = true)
    : RemixEffect(RemixEffectKind.Bass, IsEnabled);

public sealed record EqualizerEffect(double LowGainDb, double MidGainDb, double HighGainDb, bool IsEnabled = true)
    : RemixEffect(RemixEffectKind.Equalizer, IsEnabled);

public sealed record ReverbEffect(double Mix, double DecaySeconds, double RoomSize = 0.6, bool IsEnabled = true)
    : RemixEffect(RemixEffectKind.Reverb, IsEnabled);

public sealed record EchoEffect(double DelayMilliseconds, double Feedback, double Mix, bool IsEnabled = true)
    : RemixEffect(RemixEffectKind.Echo, IsEnabled);

public sealed record VolumeEffect(double GainDb, bool IsEnabled = true)
    : RemixEffect(RemixEffectKind.Volume, IsEnabled);

public sealed record FadeInEffect(double DurationSeconds, bool IsEnabled = true)
    : RemixEffect(RemixEffectKind.FadeIn, IsEnabled);

public sealed record FadeOutEffect(double DurationSeconds, bool IsEnabled = true)
    : RemixEffect(RemixEffectKind.FadeOut, IsEnabled);

public sealed record LoudnessNormalizeEffect(double TargetLufs, bool IsEnabled = true)
    : RemixEffect(RemixEffectKind.LoudnessNormalize, IsEnabled);

public enum CoverArtAction
{
    Keep,
    Replace,
    Remove,
}

public sealed record RemixMetadata(
    string? Title = null,
    string? Artist = null,
    string? Album = null,
    string? AlbumArtist = null,
    string? Genre = null,
    string? Date = null,
    string? Track = null,
    string? Disc = null,
    string? Comment = null,
    IReadOnlyDictionary<string, string>? CustomTags = null,
    CoverArtAction CoverAction = CoverArtAction.Keep,
    string? CoverPath = null);
