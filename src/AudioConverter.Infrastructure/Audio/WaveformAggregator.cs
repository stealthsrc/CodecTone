namespace AudioConverter.Infrastructure.Audio;

public static class WaveformAggregator
{
    public static double[] Aggregate(ReadOnlySpan<short> samples, int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        var peaks = new double[width];
        if (samples.IsEmpty)
        {
            return peaks;
        }
        for (var index = 0; index < width; index++)
        {
            var start = index * samples.Length / width;
            var end = Math.Min(Math.Max((index + 1) * samples.Length / width, start + 1), samples.Length);
            var peak = 0;
            for (var sampleIndex = start; sampleIndex < end; sampleIndex++)
            {
                var absolute = samples[sampleIndex] == short.MinValue ? 32768 : Math.Abs(samples[sampleIndex]);
                peak = Math.Max(peak, absolute);
            }
            peaks[index] = Math.Min(peak / 32768d, 1);
        }
        return peaks;
    }
}
