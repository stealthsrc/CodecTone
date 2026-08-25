using AudioConverter.Infrastructure.Audio;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class WaveformAggregatorTests
{
    [TestMethod]
    public void Aggregate_CreatesNormalizedPeakForEveryPixel()
    {
        short[] samples = [0, 16384, -32768, 8192];

        var peaks = WaveformAggregator.Aggregate(samples, 2);

        CollectionAssert.AreEqual(new[] { 0.5, 1.0 }, peaks);
    }

    [TestMethod]
    public void Aggregate_ReturnsSilenceForNoSamples()
    {
        CollectionAssert.AreEqual(new[] { 0.0, 0.0, 0.0 }, WaveformAggregator.Aggregate([], 3));
    }
}
