namespace AudioConverter.Core.Compression;

public sealed record CompressionSavingsResult(long SavedBytes, double ReductionPercent);

public static class CompressionSavings
{
    public static CompressionSavingsResult Calculate(long originalProcessedBytes, long outputBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(originalProcessedBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(outputBytes);
        var saved = Math.Max(0, originalProcessedBytes - outputBytes);
        var reduction = originalProcessedBytes == 0 ? 0 : saved * 100d / originalProcessedBytes;
        return new CompressionSavingsResult(saved, reduction);
    }
}
