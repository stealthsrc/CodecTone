namespace AudioConverter.Core.Remix;

public static class RemixRackValidator
{
    private const int MaximumEffects = 16;

    public static void Validate(IReadOnlyList<RemixEffect> effects, double sourceDurationSeconds)
    {
        ArgumentNullException.ThrowIfNull(effects);
        if (!double.IsFinite(sourceDurationSeconds) || sourceDurationSeconds <= 0)
            throw new ArgumentException("Source duration must be positive.", nameof(sourceDurationSeconds));
        if (effects.Count > MaximumEffects) throw new ArgumentException($"A remix rack supports at most {MaximumEffects} effects.");

        var active = effects.Where(effect => effect.Enabled).ToArray();
        RejectDuplicate<TempoPitchEffect>(active, "Tempo/Pitch");
        RejectDuplicate<LoudnessNormalizeEffect>(active, "Loudness Normalize");
        RejectDuplicate<FadeInEffect>(active, "Fade In");
        RejectDuplicate<FadeOutEffect>(active, "Fade Out");

        foreach (var effect in active) ValidateEffect(effect, sourceDurationSeconds);

        var normalizeIndex = Array.FindIndex(active, effect => effect is LoudnessNormalizeEffect);
        if (normalizeIndex >= 0 && active.Skip(normalizeIndex + 1).Any(IsGainOrToneEffect))
            throw new ArgumentException("Loudness Normalize must follow gain and tone effects.");

        var firstFade = Array.FindIndex(active, effect => effect is FadeInEffect or FadeOutEffect);
        if (firstFade >= 0 && active.Skip(firstFade).Any(effect => effect is not FadeInEffect and not FadeOutEffect))
            throw new ArgumentException("Fade effects must remain at the end of the rack.");
    }

    public static double CalculateOutputDuration(double sourceDurationSeconds, IReadOnlyList<RemixEffect> effects)
    {
        var rate = effects.OfType<TempoPitchEffect>().FirstOrDefault(effect => effect.Enabled)?.Rate ?? 1;
        return sourceDurationSeconds / rate;
    }

    private static void ValidateEffect(RemixEffect effect, double sourceDuration)
    {
        static void Require(double value, double minimum, double maximum, string name)
        {
            if (!double.IsFinite(value) || value < minimum || value > maximum)
                throw new ArgumentException($"{name} must be between {minimum} and {maximum}.");
        }

        switch (effect)
        {
            case TempoPitchEffect value: Require(value.Rate, 0.5, 2, "Tempo/Pitch rate"); break;
            case BassEffect value:
                Require(value.GainDb, -12, 18, "Bass gain");
                Require(value.FrequencyHz, 40, 250, "Bass frequency");
                break;
            case EqualizerEffect value:
                Require(value.LowGainDb, -18, 18, "Low EQ gain");
                Require(value.MidGainDb, -18, 18, "Mid EQ gain");
                Require(value.HighGainDb, -18, 18, "High EQ gain");
                break;
            case ReverbEffect value:
                Require(value.Mix, 0, 1, "Reverb mix");
                Require(value.DecaySeconds, 0.2, 8, "Reverb decay");
                Require(value.RoomSize, 0, 1, "Reverb room size");
                break;
            case EchoEffect value:
                Require(value.DelayMilliseconds, 20, 2000, "Echo delay");
                Require(value.Feedback, 0, 0.9, "Echo feedback");
                Require(value.Mix, 0, 1, "Echo mix");
                break;
            case VolumeEffect value: Require(value.GainDb, -24, 12, "Volume gain"); break;
            case FadeInEffect value: Require(value.DurationSeconds, 0, Math.Min(30, sourceDuration), "Fade In duration"); break;
            case FadeOutEffect value: Require(value.DurationSeconds, 0, Math.Min(30, sourceDuration), "Fade Out duration"); break;
            case LoudnessNormalizeEffect value: Require(value.TargetLufs, -24, -8, "Loudness target"); break;
        }
    }

    private static bool IsGainOrToneEffect(RemixEffect effect) =>
        effect is BassEffect or EqualizerEffect or ReverbEffect or EchoEffect or VolumeEffect;

    private static void RejectDuplicate<T>(IEnumerable<RemixEffect> effects, string name)
        where T : RemixEffect
    {
        if (effects.OfType<T>().Skip(1).Any()) throw new ArgumentException($"{name} can appear only once.");
    }
}
