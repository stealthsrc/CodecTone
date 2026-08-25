using AudioConverter.Core.Progress;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class OperationTimingTests
{
    [TestMethod]
    public void EstimateRemaining_ProjectsFromElapsedProgress()
    {
        var remaining = OperationTiming.EstimateRemaining(TimeSpan.FromSeconds(30), 0.5);

        Assert.AreEqual(TimeSpan.FromSeconds(30), remaining);
    }

    [TestMethod]
    public void EstimateRemaining_ReturnsNullBeforeProgressStarts()
    {
        Assert.IsNull(OperationTiming.EstimateRemaining(TimeSpan.FromSeconds(10), 0));
    }

    [DataTestMethod]
    [DataRow(5, "5s")]
    [DataRow(65, "1m 05s")]
    [DataRow(3665, "1h 01m")]
    public void FormatCompact_ProducesReadableDuration(int seconds, string expected)
    {
        Assert.AreEqual(expected, OperationTiming.FormatCompact(TimeSpan.FromSeconds(seconds)));
    }
}
