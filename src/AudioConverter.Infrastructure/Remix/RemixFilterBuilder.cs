using System.Globalization;
using AudioConverter.Core.Remix;

namespace AudioConverter.Infrastructure.Remix;

public sealed record LoudnessMeasurements(
    double InputIntegrated,
    double InputTruePeak,
    double InputLoudnessRange,
    double InputThreshold,
    double TargetOffset);

public static class RemixFilterBuilder
{
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
            switch (effect)
            {
                case TempoPitchEffect value:
                    filters.Add($"asetrate={Number(sampleRate * value.Rate)},aresample={sampleRate}");
                    break;
                case BassEffect value:
                    filters.Add($"bass=g={Number(value.GainDb)}:f={Number(value.FrequencyHz)}:w=0.5");
                    break;
                case EqualizerEffect value:
                    filters.Add($"equalizer=f=100:width_type=q:w=1:g={Number(value.LowGainDb)}");
                    filters.Add($"equalizer=f=1000:width_type=q:w=1:g={Number(value.MidGainDb)}");
                    filters.Add($"equalizer=f=10000:width_type=q:w=1:g={Number(value.HighGainDb)}");
                    break;
                case ReverbEffect value:
                    filters.Add(BuildReverb(value));
                    break;
                case EchoEffect value:
                    filters.Add($"aecho=0.8:{Number(1 - value.Mix / 2)}:{Number(value.DelayMilliseconds)}:{Number(value.Feedback)}");
                    break;
                case VolumeEffect value:
                    filters.Add($"volume={Number(value.GainDb)}dB");
                    break;
                case FadeInEffect value:
                    filters.Add($"afade=t=in:st=0:d={Number(value.DurationSeconds)}");
                    break;
                case FadeOutEffect value:
                    filters.Add($"afade=t=out:st={Number(outputDuration - value.DurationSeconds)}:d={Number(value.DurationSeconds)}");
                    break;
                case LoudnessNormalizeEffect value:
                    filters.Add(BuildLoudness(value, preview, loudnessMeasurements));
                    break;
            }
        }
        return string.Join(',', filters);
    }

    private static string BuildReverb(ReverbEffect effect)
    {
        var baseDelay = 35 + effect.RoomSize * 65;
        var feedback = Math.Min(0.88, 0.25 + effect.DecaySeconds / 12);
        var second = feedback * 0.72;
        var third = second * 0.72;
        return $"aecho=0.8:{Number(1 - effect.Mix / 2)}:{Number(baseDelay)}|{Number(baseDelay * 1.9)}|{Number(baseDelay * 3.1)}:{Number(feedback)}|{Number(second)}|{Number(third)}";
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
