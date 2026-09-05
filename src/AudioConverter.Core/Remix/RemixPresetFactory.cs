namespace AudioConverter.Core.Remix;

public static class RemixPresetFactory
{
    public static IReadOnlyList<RemixEffect> Create(RemixPreset preset) => preset switch
    {
        RemixPreset.BassBoost => [new BassEffect(8, 90), new VolumeEffect(-2)],
        RemixPreset.SlowedReverb => [new TempoPitchEffect(0.85), new ReverbEffect(0.28, 2.4)],
        RemixPreset.SpedUpReverb => [new TempoPitchEffect(1.18), new ReverbEffect(0.18, 1.6)],
        RemixPreset.Nightcore => [new TempoPitchEffect(1.25), new EqualizerEffect(-1, 0, 3), new ReverbEffect(0.1, 1, 0.45), new LoudnessNormalizeEffect(-12)],
        RemixPreset.DeepBass => [new BassEffect(12, 70), new EqualizerEffect(4, -1, -2), new VolumeEffect(-4), new LoudnessNormalizeEffect(-14)],
        RemixPreset.VocalBoost => [new EqualizerEffect(-2, 5, 2), new LoudnessNormalizeEffect(-14)],
        RemixPreset.DreamyReverb => [new EqualizerEffect(0, 1, -2), new ReverbEffect(0.42, 3.8, 0.85), new VolumeEffect(-1), new LoudnessNormalizeEffect(-15)],
        RemixPreset.LoFi => [new TempoPitchEffect(0.95), new EqualizerEffect(1, -2, -12), new ReverbEffect(0.12, 1, 0.35), new LoudnessNormalizeEffect(-16)],
        RemixPreset.Telephone => [new EqualizerEffect(-18, 5, -18), new VolumeEffect(-2), new LoudnessNormalizeEffect(-14)],
        RemixPreset.DeepSlowed => [new TempoPitchEffect(0.72), new EqualizerEffect(1, 0, -2), new ReverbEffect(0.32, 2.8), new SoftLimiterEffect(-1)],
        RemixPreset.HalfTime => [new TempoPitchEffect(0.5), new LowPassEffect(14_000), new SoftLimiterEffect(-1)],
        RemixPreset.WarmBass => [new BassEffect(4, 110), new EqualizerEffect(2, 0, -1), new LoudnessNormalizeEffect(-14)],
        RemixPreset.SubFocus => [new HighPassEffect(28), new BassEffect(8, 55), new VolumeEffect(-3), new LoudnessNormalizeEffect(-14)],
        RemixPreset.ClubPunch => [new HighPassEffect(30), new BassEffect(6, 75), new CompressorEffect(-18, 3.5, 2), new LoudnessNormalizeEffect(-10), new SoftLimiterEffect(-1)],
        RemixPreset.LightRoom => [new ReverbEffect(0.12, 1.2, 0.4), new SoftLimiterEffect(-1)],
        RemixPreset.WideHall => [new StereoWidthEffect(1.15), new ReverbEffect(0.34, 3.2, 0.8), new VolumeEffect(-1), new SoftLimiterEffect(-1)],
        RemixPreset.AmbientWash => [new EqualizerEffect(0, 1, -3), new LowPassEffect(16_000), new ReverbEffect(0.5, 5.2, 0.95), new LoudnessNormalizeEffect(-16), new SoftLimiterEffect(-1)],
        RemixPreset.EchoSpace => [new EchoEffect(360, 0.32, 0.24), new ReverbEffect(0.18, 2, 0.55), new SoftLimiterEffect(-1)],
        RemixPreset.VocalPresence => [new HighPassEffect(70), new EqualizerEffect(-1, 3, 2), new CompressorEffect(-20, 2.2, 1), new LoudnessNormalizeEffect(-14)],
        RemixPreset.SoftVocal => [new HighPassEffect(65), new EqualizerEffect(0, 2, -1), new CompressorEffect(-22, 1.8, 1), new ReverbEffect(0.1, 1.1, 0.4), new LoudnessNormalizeEffect(-15)],
        RemixPreset.DeMud => [new HighPassEffect(45), new EqualizerEffect(-2, -3, 1), new LoudnessNormalizeEffect(-14)],
        RemixPreset.ClearMix => [new HighPassEffect(35), new EqualizerEffect(-1, 1.5, 2), new CompressorEffect(-20, 2, 1), new LoudnessNormalizeEffect(-14), new SoftLimiterEffect(-1)],
        RemixPreset.Radio => [new HighPassEffect(180), new LowPassEffect(7_000), new EqualizerEffect(-5, 3, -6), new CompressorEffect(-20, 3, 2), new LoudnessNormalizeEffect(-14)],
        RemixPreset.VintageWarm => [new BassEffect(2.5, 120), new EqualizerEffect(1.5, 0.5, -2), new CompressorEffect(-20, 1.8, 1), new LoudnessNormalizeEffect(-15)],
        RemixPreset.DarkTone => [new EqualizerEffect(1, 0, -6), new LowPassEffect(14_000), new LoudnessNormalizeEffect(-15)],
        RemixPreset.BrightTone => [new HighPassEffect(35), new EqualizerEffect(-1, 1, 5), new LoudnessNormalizeEffect(-14), new SoftLimiterEffect(-1)],
        RemixPreset.CleanMaster => [new HighPassEffect(25), new CompressorEffect(-18, 1.8, 1), new LoudnessNormalizeEffect(-14), new SoftLimiterEffect(-1)],
        RemixPreset.LoudMaster => [new CompressorEffect(-16, 4, 3), new LoudnessNormalizeEffect(-9), new SoftLimiterEffect(-0.8)],
        RemixPreset.DynamicMaster => [new CompressorEffect(-20, 1.6, 0.5), new LoudnessNormalizeEffect(-14), new SoftLimiterEffect(-1)],
        RemixPreset.WideMaster => [new CompressorEffect(-18, 2, 1), new StereoWidthEffect(1.18), new LoudnessNormalizeEffect(-14), new SoftLimiterEffect(-1)],
        RemixPreset.StreamingMaster => [new CompressorEffect(-20, 1.8, 1), new LoudnessNormalizeEffect(-14), new SoftLimiterEffect(-1)],
        RemixPreset.ClubMaster => [new HighPassEffect(28), new BassEffect(3, 80), new CompressorEffect(-16, 3, 2), new LoudnessNormalizeEffect(-10), new SoftLimiterEffect(-0.8)],
        RemixPreset.Earrape => [new BassEffect(10, 85), new EqualizerEffect(4, 3, 2), new DistortionEffect(24, 1, 4), new CompressorEffect(-6, 8, 9), new VolumeEffect(6), new SoftLimiterEffect(-0.1)],
        _ => throw new ArgumentOutOfRangeException(nameof(preset)),
    };

    public static AdaptiveRemixRack CreateAdaptive(
        RemixPreset preset,
        RemixIntensity intensity,
        AudioAnalysis? analysis)
    {
        if (preset == RemixPreset.Earrape)
            return new AdaptiveRemixRack(Create(preset), false, "Fixed maximum preset; adaptive analysis and global intensity are bypassed.");
        var rack = Create(preset).Select(effect => ApplyIntensity(effect, intensity)).ToArray();
        if (analysis is null)
            return new AdaptiveRemixRack(rack, false, "Analysis unavailable; using safe static defaults.");

        var notes = new List<string>();
        if (analysis.LowEnergyDb - analysis.MidEnergyDb >= 4)
        {
            rack = rack.Select(effect => effect switch
            {
                BassEffect value => value with { GainDb = value.GainDb - 2.5 },
                EqualizerEffect value when value.LowGainDb > 0 => value with { LowGainDb = value.LowGainDb - 1.5 },
                _ => effect,
            }).ToArray();
            notes.Add("bass-heavy source: low-end boost reduced");
        }
        else if (analysis.MidEnergyDb - analysis.LowEnergyDb >= 5)
        {
            rack = rack.Select(effect => effect is BassEffect value ? value with { GainDb = value.GainDb + 1 } : effect).ToArray();
            notes.Add("light low end: bass treatment gently reinforced");
        }

        if (analysis.HighEnergyDb - analysis.MidEnergyDb >= 3)
        {
            rack = rack.Select(effect => effect is EqualizerEffect value && value.HighGainDb > 0
                ? value with { HighGainDb = value.HighGainDb - 1.5 }
                : effect).ToArray();
            notes.Add("bright source: treble lift moderated");
        }

        if (analysis.LoudnessRange < 5 || analysis.CrestFactorDb < 8)
        {
            rack = rack.Select(effect => effect is ReverbEffect value
                ? value with { Mix = value.Mix * 0.8 }
                : effect).ToArray();
            notes.Add("dense source: reverb reduced for clarity");
        }

        if (analysis.StereoWidth >= 0.8)
        {
            rack = rack.Select(effect => effect switch
            {
                StereoWidthEffect value => value with { Width = Math.Min(value.Width, 1.1) },
                ReverbEffect value => value with { Mix = value.Mix * 0.9 },
                _ => effect,
            }).ToArray();
            notes.Add("already wide source: spatial processing restrained");
        }

        rack = rack.Select(Clamp).ToArray();
        return new AdaptiveRemixRack(
            rack,
            true,
            notes.Count == 0 ? "Adaptive analysis applied; reference values were already suitable." : string.Join("; ", notes) + ".");
    }

    private static RemixEffect ApplyIntensity(RemixEffect effect, RemixIntensity intensity)
    {
        var scale = intensity switch { RemixIntensity.Light => 0.65, RemixIntensity.Strong => 1.25, _ => 1 };
        return effect switch
        {
            TempoPitchEffect value => value with { Rate = 1 + (value.Rate - 1) * scale },
            BassEffect value => value with { GainDb = value.GainDb * scale },
            EqualizerEffect value => value with { LowGainDb = value.LowGainDb * scale, MidGainDb = value.MidGainDb * scale, HighGainDb = value.HighGainDb * scale },
            ReverbEffect value => value with { Mix = value.Mix * scale, DecaySeconds = value.DecaySeconds * (0.8 + 0.2 * scale) },
            EchoEffect value => value with { Feedback = value.Feedback * scale, Mix = value.Mix * scale },
            VolumeEffect value => value with { GainDb = value.GainDb * scale },
            CompressorEffect value => value with { Ratio = 1 + (value.Ratio - 1) * scale, MakeupDb = value.MakeupDb * scale },
            StereoWidthEffect value => value with { Width = 1 + (value.Width - 1) * scale },
            _ => effect,
        };
    }

    private static RemixEffect Clamp(RemixEffect effect) => effect switch
    {
        TempoPitchEffect value => value with { Rate = Math.Clamp(value.Rate, 0.5, 2) },
        BassEffect value => value with { GainDb = Math.Clamp(value.GainDb, -12, 18) },
        EqualizerEffect value => value with { LowGainDb = Math.Clamp(value.LowGainDb, -18, 18), MidGainDb = Math.Clamp(value.MidGainDb, -18, 18), HighGainDb = Math.Clamp(value.HighGainDb, -18, 18) },
        ReverbEffect value => value with { Mix = Math.Clamp(value.Mix, 0, 0.7), DecaySeconds = Math.Clamp(value.DecaySeconds, 0.2, 8) },
        EchoEffect value => value with { Feedback = Math.Clamp(value.Feedback, 0, 0.8), Mix = Math.Clamp(value.Mix, 0, 0.7) },
        VolumeEffect value => value with { GainDb = Math.Clamp(value.GainDb, -24, 12) },
        CompressorEffect value => value with { Ratio = Math.Clamp(value.Ratio, 1, 10), MakeupDb = Math.Clamp(value.MakeupDb, 0, 12) },
        StereoWidthEffect value => value with { Width = Math.Clamp(value.Width, 0, 2) },
        _ => effect,
    };
}
