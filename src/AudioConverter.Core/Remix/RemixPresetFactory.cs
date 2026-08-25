namespace AudioConverter.Core.Remix;

public static class RemixPresetFactory
{
    public static IReadOnlyList<RemixEffect> Create(RemixPreset preset) => preset switch
    {
        RemixPreset.Custom => [],
        RemixPreset.BassBoost => [new BassEffect(8, 90), new VolumeEffect(-2)],
        RemixPreset.SlowedReverb => [new TempoPitchEffect(0.85), new ReverbEffect(0.28, 2.4)],
        RemixPreset.SpedUpReverb => [new TempoPitchEffect(1.18), new ReverbEffect(0.18, 1.6)],
        RemixPreset.Nightcore => [new TempoPitchEffect(1.25), new EqualizerEffect(-1, 0, 3), new ReverbEffect(0.1, 1, 0.45), new LoudnessNormalizeEffect(-12)],
        RemixPreset.DeepBass => [new BassEffect(12, 70), new EqualizerEffect(4, -1, -2), new VolumeEffect(-4), new LoudnessNormalizeEffect(-14)],
        RemixPreset.VocalBoost => [new EqualizerEffect(-2, 5, 2), new LoudnessNormalizeEffect(-14)],
        RemixPreset.DreamyReverb => [new EqualizerEffect(0, 1, -2), new ReverbEffect(0.42, 3.8, 0.85), new VolumeEffect(-1), new LoudnessNormalizeEffect(-15)],
        RemixPreset.LoFi => [new TempoPitchEffect(0.95), new EqualizerEffect(1, -2, -12), new ReverbEffect(0.12, 1, 0.35), new LoudnessNormalizeEffect(-16)],
        RemixPreset.Club => [new BassEffect(7, 75), new EqualizerEffect(3, 0, 2), new LoudnessNormalizeEffect(-10)],
        RemixPreset.AcousticWarmth => [new BassEffect(3, 120), new EqualizerEffect(2, 2, -1), new ReverbEffect(0.1, 1.2, 0.45), new LoudnessNormalizeEffect(-16)],
        RemixPreset.Telephone => [new EqualizerEffect(-18, 5, -18), new VolumeEffect(-2), new LoudnessNormalizeEffect(-14)],
        _ => throw new ArgumentOutOfRangeException(nameof(preset)),
    };
}
