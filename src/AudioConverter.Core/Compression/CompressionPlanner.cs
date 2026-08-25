using AudioConverter.Core.Models;

namespace AudioConverter.Core.Compression;

public static class CompressionPlanner
{
    private const long ContainerOverheadBytes = 32 * 1024;
    private const long OptimizedCoverBytes = 160 * 1024;
    private const long OriginalCoverReserveBytes = 512 * 1024;

    public static CompressionPlan Create(
        IReadOnlyList<CompressionSource> sources,
        CompressionOptions options)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(options);
        if (sources.Count == 0) throw new ArgumentException("At least one source is required.", nameof(sources));
        if (options.OutputFormat == AudioFormat.Wav) throw new ArgumentException("WAV is not a compression destination.");
        if (sources.Any(source => source.DurationSeconds <= 0 || source.SizeBytes <= 0))
            throw new ArgumentException("Every compression source must have a positive duration and size.");

        var totalDuration = sources.Sum(source => source.DurationSeconds);
        var originalBytes = sources.Sum(source => source.SizeBytes);
        var overhead = sources.Sum(source => EstimatedOverhead(source, options));
        var minimumTarget = 0L;
        int? targetBitrate = null;

        if (options.Profile == CompressionProfile.TargetTotalSize)
        {
            if (options.OutputFormat == AudioFormat.Flac)
                throw new ArgumentException("A target total size cannot be guaranteed for lossless FLAC output.");
            if (options.TargetTotalBytes is null or <= 0)
                throw new ArgumentException("A positive target total size is required.");

            var minimumBitrate = CompressionProfiles.MinimumBitrateKbps(options.OutputFormat);
            minimumTarget = AudioBytes(totalDuration, minimumBitrate) + overhead;
            if (options.TargetTotalBytes < minimumTarget)
                throw new ArgumentException($"Target size is below the minimum achievable size of {minimumTarget} bytes.");

            var audioBudget = options.TargetTotalBytes.Value - overhead;
            var calculated = (int)Math.Floor(audioBudget * 8d / totalDuration / 1000d);
            targetBitrate = Math.Min(calculated, CompressionProfiles.MaximumBitrateKbps(options.OutputFormat));
        }

        var files = sources.Select(source => PlanFile(source, options, targetBitrate)).ToArray();
        var estimated = options.Profile == CompressionProfile.TargetTotalSize
            ? options.TargetTotalBytes!.Value
            : files.Where(file => !file.ShouldSkip).Sum(file => file.EstimatedOutputBytes);

        return new CompressionPlan(
            options,
            files,
            targetBitrate,
            originalBytes,
            estimated,
            minimumTarget,
            totalDuration);
    }

    private static CompressionPlannedFile PlanFile(
        CompressionSource source,
        CompressionOptions options,
        int? targetBitrate)
    {
        var estimated = options.OutputFormat == AudioFormat.Flac
            ? EstimateFlac(source, options.Profile)
            : AudioBytes(
                source.DurationSeconds,
                targetBitrate ?? CompressionProfiles.NominalBitrateKbps(options.OutputFormat, options.Profile))
              + EstimatedOverhead(source, options);
        var skip = estimated >= source.SizeBytes * 0.98;
        return new CompressionPlannedFile(
            source,
            estimated,
            skip,
            skip ? "Skipped because no meaningful size reduction is expected." : null);
    }

    private static long EstimateFlac(CompressionSource source, CompressionProfile profile)
    {
        var factor = source.Format switch
        {
            AudioFormat.Wav => 0.55,
            AudioFormat.Flac => profile switch
            {
                CompressionProfile.HighFidelity => 0.99,
                CompressionProfile.Balanced => 0.98,
                CompressionProfile.MaximumReduction => 0.97,
                _ => 1,
            },
            _ => 1.5,
        };
        return (long)Math.Ceiling(source.SizeBytes * factor);
    }

    private static long EstimatedOverhead(CompressionSource source, CompressionOptions options) =>
        ContainerOverheadBytes + (source.HasCoverArt && options.PreserveMetadata
            ? options.OptimizeArtwork ? OptimizedCoverBytes : OriginalCoverReserveBytes
            : 0);

    private static long AudioBytes(double durationSeconds, int bitrateKbps) =>
        (long)Math.Ceiling(durationSeconds * bitrateKbps * 1000d / 8d);
}
