namespace AudioConverter.Infrastructure.Audio;

public static class WaveformAggregator
{
    public static async Task<double[]> AggregateAsync(Stream stream, int width, long expectedSamples, CancellationToken token = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedSamples);
        var peaks = new double[width];
        var buffer = new byte[65536];
        long sampleIndex = 0;
        var pending = 0;
        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(pending), token).ConfigureAwait(false);
            if (read == 0) break;
            var count = pending + read;
            var even = count - count % 2;
            for (var offset = 0; offset < even; offset += 2)
            {
                var sample = System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(offset, 2));
                var bucket = (int)Math.Min(width - 1, sampleIndex++ * (double)width / expectedSamples);
                peaks[bucket] = Math.Max(peaks[bucket], Math.Abs((double)sample) / 32768);
            }
            pending = count - even;
            if (pending != 0) buffer[0] = buffer[even];
        }
        return peaks;
    }

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
