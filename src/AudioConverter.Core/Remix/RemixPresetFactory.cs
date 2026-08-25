namespace AudioConverter.Core.Remix;

public static class RemixPresetFactory
{
    public static IReadOnlyList<RemixEffect> Create(RemixPreset preset) => preset switch
    {
        RemixPreset.Custom => [],
        RemixPreset.BassBoost => [new BassEffect(8, 90), new VolumeEffect(-2)],
        RemixPreset.SlowedReverb => [new TempoPitchEffect(0.85), new ReverbEffect(0.28, 2.4)],
        RemixPreset.SpedUpReverb => [new TempoPitchEffect(1.18), new ReverbEffect(0.18, 1.6)],
        _ => throw new ArgumentOutOfRangeException(nameof(preset)),
    };
}
