using System.Globalization;
using AudioConverter.Core.Remix;

namespace AudioConverter.Infrastructure.Remix;

public sealed record LoudnessMeasurements(
    double InputIntegrated,
    double InputTruePeak,
    double InputLoudnessRange,
    double InputThreshold,
    double TargetOffset);

public sealed record RemixFilterGraph(string Graph, string OutputLabel);

public static class RemixFilterBuilder
{
    private const double ImpulseResponseSeconds = 3.5;

    public static RemixFilterGraph BuildGraph(
        IReadOnlyList<RemixEffect> effects,
        int sampleRate,
        double sourceDurationSeconds,
        bool preview,
        IReadOnlyList<int> reverbInputIndexes,
        LoudnessMeasurements? loudnessMeasurements = null)
    {
        RemixRackValidator.Validate(effects, sourceDurationSeconds);
        var active = effects.Where(effect => effect.Enabled).ToArray();
        var reverbCount = active.Count(effect => effect is ReverbEffect);
        if (reverbInputIndexes.Count != reverbCount)
            throw new ArgumentException("One impulse-response input is required for each active reverb effect.");

        var outputDuration = RemixRackValidator.CalculateOutputDuration(sourceDurationSeconds, effects);
        var parts = new List<string>();
        var current = "0:a";
        var audioIndex = 0;
        var reverbIndex = 0;
        foreach (var effect in active)
        {
            if (effect is ReverbEffect reverb)
            {
                var irLabel = $"ir{reverbIndex}";
                parts.Add($"[{reverbInputIndexes[reverbIndex]}:a]aresample={sampleRate}[{irLabel}]");
                var dryLabel = $"dry{reverbIndex}";
                var wetInputLabel = $"wetin{reverbIndex}";
                parts.Add($"[{current}]asplit=2[{dryLabel}][{wetInputLabel}]");
                var wetLabel = $"wet{reverbIndex}";
                parts.Add($"[{wetInputLabel}][{irLabel}]{BuildConvolutionReverb(reverb)}[{wetLabel}]");
                var convolutionLabel = $"remix{audioIndex++}";
                parts.Add($"[{dryLabel}][{wetLabel}]amix=inputs=2:weights='{Number(1 - reverb.Mix)} {Number(reverb.Mix)}':normalize=0[{convolutionLabel}]");
                var limitedLabel = $"remix{audioIndex++}";
                parts.Add($"[{convolutionLabel}]alimiter=limit=0.84:level=false[{limitedLabel}]");
                current = limitedLabel;
                reverbIndex++;
                continue;
            }

            var filter = BuildEffect(effect, sampleRate, outputDuration, preview, loudnessMeasurements);
            var next = $"remix{audioIndex++}";
            parts.Add($"[{current}]{filter}[{next}]");
            current = next;
        }
        parts.Add($"[{current}]anull[remixout]");
        return new RemixFilterGraph(string.Join(';', parts), "remixout");
    }

    public static string Build(
        IReadOnlyList<RemixEffect> effects,
        int sampleRate,
        double sourceDurationSeconds,
        bool preview,
        LoudnessMeasurements? loudnessMeasurements = null)
    {
        RemixRackValidator.Validate(effects, sourceDurationSeconds);
        var outputDuration = RemixRackValidator.CalculateOutputDuration(sourceDurationSeconds, effects);
        var filters = new List<string>();
        foreach (var effect in effects.Where(effect => effect.Enabled))
        {
            if (effect is ReverbEffect)
                throw new ArgumentException("Reverb requires BuildGraph and an impulse-response input.");
            filters.Add(BuildEffect(effect, sampleRate, outputDuration, preview, loudnessMeasurements));
        }
        return string.Join(',', filters);
    }

    private static string BuildEffect(
        RemixEffect effect,
        int sampleRate,
        double outputDuration,
        bool preview,
        LoudnessMeasurements? measurement) => effect switch
    {
        TempoPitchEffect value => $"asetrate={Number(sampleRate * value.Rate)},aresample={sampleRate}:filter_size=64:phase_shift=10",
        BassEffect value => $"bass=g={Number(value.GainDb)}:f={Number(value.FrequencyHz)}:w=0.5",
        EqualizerEffect value => $"equalizer=f=100:width_type=q:w=1:g={Number(value.LowGainDb)},equalizer=f=1000:width_type=q:w=1:g={Number(value.MidGainDb)},equalizer=f=10000:width_type=q:w=1:g={Number(value.HighGainDb)}",
        EchoEffect value => $"aecho=0.8:{Number(Math.Min(0.7, 0.25 + value.Mix * 0.35))}:{Number(value.DelayMilliseconds)}:{Number(value.Feedback)}",
        VolumeEffect value => $"volume={Number(value.GainDb)}dB",
        FadeInEffect value => $"afade=t=in:st=0:d={Number(value.DurationSeconds)}",
        FadeOutEffect value => $"afade=t=out:st={Number(outputDuration - value.DurationSeconds)}:d={Number(value.DurationSeconds)}",
        LoudnessNormalizeEffect value => BuildLoudness(value, preview, measurement),
        _ => "anull",
    };

    private static string BuildConvolutionReverb(ReverbEffect effect)
    {
        var length = Math.Clamp(effect.DecaySeconds / ImpulseResponseSeconds, 0.1, 1);
        var impulseGain = 0.35 + effect.RoomSize * 0.45;
        return $"afir=dry=1:wet=1:length={Number(length)}:irgain={Number(impulseGain)}:irnorm=1:precision=double";
    }

    private static string BuildLoudness(
        LoudnessNormalizeEffect effect,
        bool preview,
        LoudnessMeasurements? measurement)
    {
        var prefix = $"loudnorm=I={Number(effect.TargetLufs)}:TP=-1.5:LRA=11";
        if (preview || measurement is null) return prefix;
        return prefix
            + $":measured_I={Number(measurement.InputIntegrated)}"
            + $":measured_TP={Number(measurement.InputTruePeak)}"
            + $":measured_LRA={Number(measurement.InputLoudnessRange)}"
            + $":measured_thresh={Number(measurement.InputThreshold)}"
            + $":offset={Number(measurement.TargetOffset)}:linear=true";
    }

    private static string Number(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);
}
