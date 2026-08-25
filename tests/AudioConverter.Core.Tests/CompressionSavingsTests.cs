using AudioConverter.Core.Compression;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class CompressionSavingsTests
{
    [TestMethod]
    public void Calculate_UsesOnlySuccessfullyProcessedSourceBytes()
    {
        var savings = CompressionSavings.Calculate(10_000_000, 6_000_000);

        Assert.AreEqual(4_000_000, savings.SavedBytes);
        Assert.AreEqual(40, savings.ReductionPercent, 0.001);
    }

    [TestMethod]
    public void Calculate_ReportsZeroWhenEverythingWasSkipped()
    {
        var savings = CompressionSavings.Calculate(0, 0);

        Assert.AreEqual(0, savings.SavedBytes);
        Assert.AreEqual(0, savings.ReductionPercent);
    }
}
